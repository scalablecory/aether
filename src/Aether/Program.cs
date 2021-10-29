using Aether.Devices.Drivers;
using Aether.Devices.Sensors;
using Aether.Devices.Sensors.Metadata;
using Aether.Devices.Simulated;
using Aether.Reactive;
using Aether.Themes;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UnitsNet;

// TODO: this needs to be replaced by something that reads config, resolves dependencies, etc.
var runDeviceCommand = new Command("run-device", "Runs an Aether device");

runDeviceCommand.Handler = CommandHandler.Create(async () =>
{
    // Initialize MS5637.
    using I2cDevice ms5637Device = I2cDevice.Create(new I2cConnectionSettings(1, ObservableMs5637.DefaultAddress));
    await using ObservableSensor ms5637Driver = ObservableMs5637.OpenSensor(ms5637Device, dependencies: Observable.Empty<Measurement>());

    // Initialize SCD4x, taking a dependency on MS5637 for calibration with barometric pressure.
    using I2cDevice scd4xDevice = I2cDevice.Create(new I2cConnectionSettings(1, ObservableScd4x.DefaultAddress));
    await using ObservableSensor scdDriver = ObservableScd4x.OpenSensor(scd4xDevice, dependencies: ms5637Driver);

    // Initialize SHT4x
    // using I2cDevice sht4xDevice = I2cDevice.Create(new I2cConnectionSettings(1, ObservableSht4x.DefaultAddress));
    // await using ObservableSensor shtDriver = ObservableSht4x.OpenSensor(sht4xDevice, dependencies: Observable.Empty<Measurement>());

    // Initialize SGP4x, taking a dependency on SCD4x for temperature and relative humidity
    using I2cDevice sgp4xDevice = I2cDevice.Create(new I2cConnectionSettings(1, ObservableSgp4x.DefaultAddress));
    await using ObservableSensor sgpDriver = ObservableSgp4x.OpenSensor(sgp4xDevice, dependencies: scdDriver);

    // All the measurements funnel through here.
    // Multiple sensors can support the same measures. In this case, both devices support teperature. To prevent inconsistencies, only use one.
    IObservable<Measurement> measurements = Observable.Merge(
        ms5637Driver.Where(x => x.Measure == Measure.BarometricPressure),
        scdDriver,
        sgpDriver
        );

    // Initialize display.
    var spiConfig = new System.Device.Spi.SpiConnectionSettings(0, 0)
    {
        ClockFrequency = 10000000
    };
    using var gpio = new GpioController(PinNumberingScheme.Logical);
    using SpiDevice displayDevice = SpiDevice.Create(spiConfig);
    using var displayDriver = new WaveshareEPD2_9inV2(displayDevice, gpio, dcPinId: 25, rstPinId: 17, busyPinId: 24);

    // Initialize the theme, which takes all the measurements and renders them to a display.
    var lines = new[] { Measure.CO2, Measure.Humidity, Measure.BarometricPressure, Measure.Temperature, Measure.VOC };
    using IDisposable theme = MultiLineTheme.CreateTheme(displayDriver, lines, measurements);

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
    using IDisposable theme = MultiLineTheme.CreateTheme(driver, lines, sub);

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
var displayTestCommand = new Command("display-test", "Tests a display.");
displayTestCommand.Handler = CommandHandler.Create(() =>
{
    var spiConfig = new System.Device.Spi.SpiConnectionSettings(0, 0)
    {
        ClockFrequency = 10000000
    };

    var lines = new[] { Measure.CO2, Measure.Humidity, Measure.BarometricPressure, Measure.Temperature };

    using var gpio = new GpioController(PinNumberingScheme.Logical);
    using SpiDevice device = SpiDevice.Create(spiConfig);
    using var driver = new WaveshareEPD2_9inV2(device, gpio, dcPinId: 25, rstPinId: 17, busyPinId: 24);

    using var sub = new Subject<Measurement>();
    using IDisposable theme = MultiLineTheme.CreateTheme(driver, lines, sub);

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
    displayTestCommand
};

await rootCommand.InvokeAsync(Environment.CommandLine);

static Task RunAndPrintSensorAsync(Func<ObservableSensor> sensorFunc) =>
    AetherObservable.AsyncUsing(sensorFunc, sensor => sensor)
    .TakeUntil(AetherObservable.ConsoleCancelKeyPress)
    .ForEachAsync(measurement =>
    {
        Console.WriteLine($"[{DateTime.Now:t}] {measurement}");
    });
