using Aether.Devices.Sensors.Metadata;
using System.Device.I2c;
using UnitsNet;

namespace Aether.Devices.Sensors.Observable
{
    internal sealed class ObservableSHT4x : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Drivers.SHT4x _sensor;

        private ObservableSHT4x(I2cDevice device)
            : base(Measure.Humidity, Measure.Temperature)
        {
            _sensor = new Drivers.SHT4x(device);
        }

        protected override void DisposeCore() =>
            _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5_000));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                (RelativeHumidity humidity, Temperature temperature) =
                    _sensor.ReadHumidityAndTemperature();

                OnNextRelativeHumidity(humidity);
                OnNextTemperature(temperature);
            }
        }

        #region IObservableI2CSensorFactory

        public static int DefaultAddress => Drivers.SHT4x.DefaultAddress;

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

        public static ObservableSensor OpenDevice(I2cDevice device, IEnumerable<ObservableSensor> dependencies) =>
            new ObservableSHT4x(device);

        #endregion
    }
}
