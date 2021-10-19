using Aether.Devices.Sensors.Metadata;
using Iot.Device.Sht4x;
using System.Device.I2c;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal sealed class ObservableSht4x : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Sht4x _sensor;

        private ObservableSht4x(I2cDevice device)
        {
            _sensor = new Sht4x(device);
            Start();
        }

        protected override void DisposeCore() =>
            _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                (RelativeHumidity? humidity, Temperature? temperature) =
                    await _sensor.ReadHumidityAndTemperatureAsync().ConfigureAwait(false);

                if (humidity is not null) OnNextRelativeHumidity(humidity.GetValueOrDefault());
                if (temperature is not null) OnNextTemperature(temperature.GetValueOrDefault());
            }
        }

        #region IObservableI2CSensorFactory

        public static int DefaultAddress => Sht4x.DefaultI2cAddress;

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

        public static ObservableSensor OpenSensor(I2cDevice device, IObservable<Measurement> dependencies) =>
            new ObservableSht4x(device);

        #endregion
    }
}
