using System.CommandLine;
using System.CommandLine.Invocation;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Aether.Devices.Drivers;
using Aether.Devices.Sensors;
using Aether.Devices.Sensors.Metadata;
using Aether.Devices.Simulated;
using Aether.Reactive;
using Aether.Themes;
using UnitsNet;

// TODO: this needs to be replaced by something that reads config, resolves dependencies, etc.
var runDeviceCommand = new Command("run-device", "Runs an Aether device");

runDeviceCommand.Handler = CommandHandler.Create(async () =>
{
    using I2cBus bus = I2cBus.Create(busId: 1);

    // Initialize SPS30.
    // TODO: For some reason the SPS30 fails to start if it's not the first driver to write to the bus.
    using I2cDevice sps30Device = bus.CreateDevice(ObservableSps30.DefaultAddress);
    await using ObservableSensor spsDriver = ObservableSps30.OpenSensor(sps30Device, dependencies: Observable.Empty<Measurement>());

    // Initialize MS5637.
    using I2cDevice ms5637Device = bus.CreateDevice(ObservableMs5637.DefaultAddress);
    await using ObservableSensor ms5637Driver = ObservableMs5637.OpenSensor(ms5637Device, dependencies: Observable.Empty<Measurement>());

    // Initialize SCD4x, taking a dependency on MS5637 for calibration with barometric pressure.
    using I2cDevice scd4xDevice = bus.CreateDevice(ObservableScd4x.DefaultAddress);
    await using ObservableSensor scdDriver = ObservableScd4x.OpenSensor(scd4xDevice, dependencies: ms5637Driver);

    // Initialize SGP4x, taking a dependency on SCD4x for temperature and relative humidity
    using I2cDevice sgp4xDevice = bus.CreateDevice(ObservableSgp4x.DefaultAddress);
    await using ObservableSensor sgpDriver = ObservableSgp4x.OpenSensor(sgp4xDevice, dependencies: scdDriver);

    // All the measurements funnel through here.
    // Multiple sensors can support the same measures. In this case, the MS5637 and SCD4x both support temperature. To prevent inconsistencies, only use one.
    IObservable<Measurement> measurements = Observable.Merge(
        ms5637Driver.Where(x => x.Measure == Measure.BarometricPressure),
        scdDriver,
        sgpDriver,
        spsDriver
        );

    // Initialize ePaper display.
    var spiConfig = new SpiConnectionSettings(0, 0)
    {
        ClockFrequency = 10_000_000
    };
    using var gpio = new GpioController(PinNumberingScheme.Logical);
    using SpiDevice displayDevice = SpiDevice.Create(spiConfig);
    using var displayDriver = new WaveshareEPD2_9inV2(displayDevice, gpio, dcPinId: 25, rstPinId: 17, busyPinId: 24);

    // Initialize ePaper theme, which takes all the measurements and renders them to a display.
    var lines = new[] { Measure.CO2, Measure.PM1_0, Measure.PM2_5, Measure.PM4_0, Measure.PM10_0, Measure.VOC, Measure.Temperature };
    using IDisposable ePaperTheme = MultiLineTheme.Run(displayDriver, lines, measurements, vertical: true);

    // Initialize RGB display.
    spiConfig = new SpiConnectionSettings(1)
    {
        ClockFrequency = 10_000_000
    };
    using SpiDevice rgbDevice = SpiDevice.Create(spiConfig);
    using var rgbDriver = new Sk9822(rgbDevice, pixelCount: 4);

    // Initialize RGB theme, which takes all the measurements and converts them to an RGB color for LEDs.
    using IDisposable rgbTheme = RgbTheme.Run(rgbDriver, measurements);

    // Initialize console theme.
    using IDisposable consoleTheme = ConsoleTheme.Run(measurements);

    // Wait for Ctrl+C to exit.
    var tcs = new TaskCompletionSource();
    Console.CancelKeyPress += (s, e) =>
    {
        Console.WriteLine("Closing...");
        e.Cancel = true;
        tcs.TrySetResult();
    };
    await tcs.Task;
});

var listSensorCommand = new Command("list", "Lists available sensors")
{
    Handler = CommandHandler.Create(() =>
    {
        foreach (SensorInfo sensorInfo in SensorInfo.Sensors)
        {
            string type = sensorInfo switch
            {
                I2cSensorInfo i2c => $"i2c({i2c.DefaultAddress})",
                _ => throw new Exception($"Unknown {nameof(SensorInfo)} subclass.")
            };

            Console.WriteLine($"{type}{(sensorInfo.CanSimulateSensor ? " / simulatable" : "              ")} - {sensorInfo.Name} - {string.Join(", ", sensorInfo.Measures)}");
        }
    })
};

var testi2cSensorCommand = new Command("i2c", "Tests an I2C sensor")
{
    new Argument<string>("name", "The name of the sensor to test."),
    new Argument<uint>("bus", "The I2C bus to use."),
    new Argument<uint>("address", "The I2C address to use.")
};

testi2cSensorCommand.Handler = CommandHandler.Create((string name, uint bus, uint address) => RunAndPrintSensorAsync(() =>
{
    SensorInfo? sensorInfo = SensorInfo.Sensors.FirstOrDefault(x => x is I2cSensorInfo && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

    return sensorInfo switch
    {
        I2cSensorInfo i2c => i2c.OpenDevice((int)bus, (int)address, Observable.Empty<Measurement>()),
        _ => throw new Exception("An I2C sensor by that name was not found.")
    };
}));

var simulateSensorCommand = new Command("simulate", "Simulates a sensor")
{
    new Argument<string>("name", "The name of the sensor to test.")
};

simulateSensorCommand.Handler = CommandHandler.Create((string name) => RunAndPrintSensorAsync(() =>
{
    SensorInfo sensorInfo = SensorInfo.Sensors.FirstOrDefault(x => x is I2cSensorInfo { CanSimulateSensor: true } && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new Exception("A simulatable sensor by that name was not found.");

    return sensorInfo.CreateSimulatedSensor(Observable.Empty<Measurement>());
}));

// Temporary command to test the theme.
// TODO: Make this more like a list/test format similar to sensor.
var themeTestCommand = new Command("theme-test", "Tests a theme.");
themeTestCommand.Handler = CommandHandler.Create(() =>
{
    var lines = new[] { Measure.CO2, Measure.Humidity, Measure.BarometricPressure, Measure.Temperature };

    using var driver = new SimulatedDisplayDriver("out", 128, 296, 111.917383820998f, 112.399461802960f);
    using var sub = new Subject<Measurement>();
    using IDisposable theme = MultiLineTheme.Run(driver, lines, sub, vertical: false);

    sub.OnNext(Measurement.FromCo2(VolumeConcentration.FromPartsPerMillion(4312.25)));
    sub.OnNext(Measurement.FromRelativeHumidity(RelativeHumidity.FromPercent(59.1)));
    sub.OnNext(Measurement.FromPressure(Pressure.FromAtmospheres(1.04)));
    sub.OnNext(Measurement.FromTemperature(Temperature.FromDegreesFahrenheit(65.2)));
    sub.OnNext(Measurement.FromVoc(new Aether.CustomUnits.VolatileOrganicCompoundIndex(103)));
    sub.OnNext(Measurement.FromPM2_5(MassConcentration.FromMicrogramsPerCubicMeter(0.78)));
    sub.OnNext(Measurement.FromPM10_0(MassConcentration.FromMicrogramsPerCubicMeter(1.27)));
    sub.OnCompleted();
});

// Temporary command to test the display.
// TODO: Make this more like a list/test format similar to sensor.
var rgbTestCommand = new Command("rgb-test", "Tests RGB.");
rgbTestCommand.Handler = CommandHandler.Create(async () =>
{
    var spiConfig = new System.Device.Spi.SpiConnectionSettings(6)
    {
        ClockFrequency = 10_000_000
    };
    using SpiDevice device = SpiDevice.Create(spiConfig);
    using var driver = new Sk9822(device, pixelCount: 4);

    using var sub = new Subject<Measurement>();
    using IDisposable theme = RgbTheme.Run(driver, sub);

    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / 60.0));

    const double min = 500.0;
    const double max = 6100.0;
    const double adj = (max - min) / (60.0 * 5.0);

    double ppm = min;
    do
    {
        Measurement m = Measurement.FromCo2(VolumeConcentration.FromPartsPerMillion(ppm));

        await timer.WaitForNextTickAsync();

        sub.OnNext(m);

        ppm += adj;
    }
    while (ppm < max);
});

// Temporary command to test the display.
// TODO: Make this more like a list/test format similar to sensor.
var displayTestCommand = new Command("display-test", "Tests a display.");
displayTestCommand.Handler = CommandHandler.Create(() =>
{
    var spiConfig = new System.Device.Spi.SpiConnectionSettings(0, 0)
    {
        ClockFrequency = 10_000_000
    };

    var lines = new[] { Measure.CO2, Measure.Humidity, Measure.BarometricPressure, Measure.Temperature };

    using var gpio = new GpioController(PinNumberingScheme.Logical);
    using SpiDevice device = SpiDevice.Create(spiConfig);
    using var driver = new WaveshareEPD2_9inV2(device, gpio, dcPinId: 25, rstPinId: 17, busyPinId: 24);

    using var sub = new Subject<Measurement>();
    using IDisposable theme = MultiLineTheme.Run(driver, lines, sub, vertical: true);

    sub.OnNext(Measurement.FromCo2(VolumeConcentration.FromPartsPerMillion(4312.25)));
    sub.OnNext(Measurement.FromRelativeHumidity(RelativeHumidity.FromPercent(59.1)));
    sub.OnNext(Measurement.FromPressure(Pressure.FromAtmospheres(1.04)));
    sub.OnNext(Measurement.FromTemperature(Temperature.FromDegreesFahrenheit(65.2)));
    sub.OnCompleted();
});

var rootCommand = new RootCommand()
{
    runDeviceCommand,
    new Command("sensor", "Operates on sensors")
    {
        listSensorCommand,
        new Command("test", "Tests a sensor")
        {
            testi2cSensorCommand,
        },
        simulateSensorCommand
    },
    themeTestCommand,
    rgbTestCommand,
    displayTestCommand
};

await rootCommand.InvokeAsync(Environment.CommandLine);

static Task RunAndPrintSensorAsync(Func<ObservableSensor> sensorFunc) =>
    AetherObservable.AsyncUsing(sensorFunc, sensor => sensor)
    .TakeUntil(AetherObservable.ConsoleCancelKeyPress)
    .ForEachAsync(measurement =>
    {
        Console.WriteLine($"[{DateTime.Now:t}] {measurement.Measure}: {measurement}");
    });
