using Aether.Devices.I2C;
using Aether.Devices.Sensors.Metadata;

namespace Aether.Devices.Sensors.Observable
{
    internal sealed class ObservableSHT4x : ObservableSensor, IObservableI2CSensorFactory
    {
        private readonly SHT4x _sensor;

        public static int DefaultAddress => 0x44;

        public static string Manufacturer => "Sensirion";

        public static string Name => "SHT4x";

        public static string Uri => "https://www.sensirion.com/en/environmental-sensors/humidity-sensors/humidity-sensor-sht4x/";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public static IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenDevice(I2CDevice device, IObservable<Measurement> dependencies) =>
            new ObservableSHT4x(device);

        private ObservableSHT4x(I2C.I2CDevice device)
        {
            _sensor = new SHT4x(device);
        }

        protected override void DisposeCore() =>
            _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5_000));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                (float humidity, float temperature) =
                    await _sensor.ReadHighlyRepeatableMeasurementAsync(cancellationToken).ConfigureAwait(false);

                OnNext(Measure.Humidity, humidity);
                OnNext(Measure.Temperature, temperature);
            }
        }
    }
}
