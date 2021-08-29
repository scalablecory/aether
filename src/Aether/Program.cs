using Aether.Devices.Sensors;
using Aether.Devices.Sensors.Metadata;
using Aether.Devices.Sensors.Observable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Device.I2c;
using System.Reactive.Linq;

var rootCommand = new RootCommand("Runs Aether");

var testCommand = new Command("test");
rootCommand.AddCommand(testCommand);

var testSensorCommand = new Command("sensor")
{
    new Argument<string>("name"),
    new Option<uint>("i2cBus"),
    new Option<uint>("i2cAddress")
};
testSensorCommand.Handler = CommandHandler.Create(async (string name, uint? i2cBus, uint? i2cAddress) =>
{
    SensorInfo? sensorInfo = SensorInfo.Sensors.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

    await using ObservableSensor sensor = sensorInfo switch
    {
        I2CSensorInfo i2c => CreateI2C(i2c),
        null => throw new Exception("Invalid sensor"),
        _ => throw new Exception("Unknown sensor type.")
    };

    ObservableSensor CreateI2C(I2CSensorInfo i2c)
    {
        if (i2cBus is null) throw new Exception($"Sensor '{name}' requires {nameof(i2cBus)} to be specified, and optionally {nameof(i2cAddress)}.");

        I2cDevice device = I2cDevice.Create(new I2cConnectionSettings((int)i2cBus, (int?)i2cAddress ?? i2c.DefaultAddress));
        try
        {
            return i2c.OpenDevice(device, Observable.Empty<Measurement>());
        }
        catch
        {
            device.Dispose();
            throw;
        }
    }

    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        _ = sensor.DisposeAsync().AsTask();
    };

    await sensor.ForEachAsync(measurement =>
    {
        Console.WriteLine($"[{DateTime.Now:T}] {measurement.Measure}: {measurement.Value}");
    });

    await Task.Delay(1);
});
testCommand.AddCommand(testSensorCommand);

await rootCommand.InvokeAsync(Environment.CommandLine);
