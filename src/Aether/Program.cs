using Aether.Devices.Sensors;
using Aether.Devices.Sensors.Metadata;
using Aether.Devices.Sensors.Observable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reactive.Linq;
using UnitsNet;

var listSensorCommand = new Command("list", "Lists available sensors")
{
    Handler = CommandHandler.Create(() =>
    {
        foreach (SensorInfo sensorInfo in SensorInfo.Sensors)
        {
            string type = sensorInfo switch
            {
                I2cSensorInfo i2c => $"i2c(0x{i2c.DefaultAddress:X2})",
                _ => throw new Exception($"Unknown {nameof(SensorInfo)} subclass.")
            };

            Console.WriteLine($"{type} - {sensorInfo.Name} - {string.Join(", ", sensorInfo.Measures)}");
        }
    })
};

var testi2cSensorCommand = new Command("i2c", "Tests a I2C sensor")
{
    new Argument<string>("name", "The name of the sensor to test."),
    new Argument<uint>("bus", "The I2C bus to use."),
    new Argument<uint>("address", "The I2C address to use.")
};

testi2cSensorCommand.Handler = CommandHandler.Create(async (string name, uint bus, uint address) =>
{
    SensorInfo? sensorInfo = SensorInfo.Sensors.FirstOrDefault(x => x is I2cSensorInfo && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

    await using ObservableSensor sensor = sensorInfo switch
    {
        I2cSensorInfo i2c => i2c.OpenDevice((int)bus, (int)address, Enumerable.Empty<ObservableSensor>()),
        _ => throw new Exception("Invalid sensor")
    };

    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        _ = sensor.DisposeAsync().AsTask();
    };

    await Observable.Merge(
        sensor.CO2.Select(x => (Measure.CO2, (IQuantity)x)),
        sensor.Temperature.Select(x => (Measure.Temperature, (IQuantity)x)),
        sensor.RelativeHumidity.Select(x => (Measure.Humidity, (IQuantity)x)),
        sensor.BarometricPressure.Select(x => (Measure.Pressure, (IQuantity)x))
    ).ForEachAsync(x =>
    {
        Console.WriteLine($"[{DateTime.Now:t}] {x.Item1}: {x.Item2}");
    });

    await Task.Delay(1);
});

var rootCommand = new RootCommand()
{
    new Command("sensor", "Operates on sensors")
    {
        listSensorCommand,
        new Command("test", "Tests a sensor")
        {
            testi2cSensorCommand,
        }
    }
};

await rootCommand.InvokeAsync(Environment.CommandLine);
