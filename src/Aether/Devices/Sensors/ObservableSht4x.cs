using Aether.Devices.Sensors.Metadata;
using Iot.Device.Sht4x;
using System.Device.I2c;
using System.Reactive.Linq;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal sealed class ObservableSht4x : I2cSensorFactory
    {
        public static ObservableSht4x Instance { get; } = new ObservableSht4x();

        public override int DefaultAddress => Sht4x.DefaultI2cAddress;

        public override string Manufacturer => "Sensirion";

        public override string Name => "SHT4x";

        public override string Uri => "https://www.sensirion.com/en/environmental-sensors/humidity-sensors/humidity-sensor-sht4x/";

        public override IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public override IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public override IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public override IObservable<Measurement> OpenSensor(Func<I2cDevice> deviceFunc, IObservable<Measurement> dependencies) =>
            Observable.Create(async (IObserver<Measurement> measurements, CancellationToken cancellationToken) =>
            {
                using I2cDevice device = deviceFunc();
                using var sensor = new Sht4x(device);
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
                using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

                while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                {
                    (RelativeHumidity? humidity, Temperature? temperature) =
                        await sensor.ReadHumidityAndTemperatureAsync().ConfigureAwait(false);

                    if (humidity is not null) measurements.OnNext(Measurement.FromRelativeHumidity(humidity.GetValueOrDefault()));
                    if (temperature is not null) measurements.OnNext(Measurement.FromTemperature(temperature.GetValueOrDefault()));
                }
            });
    }
}
