using System.Device.I2c;
using System.Reactive.Linq;
using Aether.CustomUnits;
using Aether.Devices.Sensors.Metadata;
using Aether.Reactive;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal sealed class ObservableSgp4x : I2cSensorFactory
    {
        public static ObservableSgp4x Instance { get; } = new ObservableSgp4x();

        public override int DefaultAddress => Drivers.Sgp4x.DefaultI2cAddress;

        public override string Manufacturer => "Sensirion";

        public override string Name => "SGP4x";

        public override string Uri => "https://www.sensirion.com/en/environmental-sensors/gas-sensors/sgp40/";

        public override IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.VOC)
        };

        public override IEnumerable<SensorDependency> Dependencies { get; } = new[]
        {
            new SensorDependency(Measure.Temperature, required: false),
            new SensorDependency(Measure.Humidity, required: false)
        };

        public override IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public override IObservable<Measurement> OpenSensor(Func<I2cDevice> deviceFunc, IObservable<Measurement> dependencies) =>
            Observable.Create(async (IObserver<Measurement> measurements, CancellationToken cancellationToken) =>
            {
                using I2cDevice device = deviceFunc();
                using var sensor = new Drivers.Sgp4x(device);
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
                using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

                var humidityObserver = new ObservedValue<RelativeHumidity>();
                using IDisposable humididtySubscription = dependencies
                    .Where(m => m.Measure == Measure.Humidity)
                    .Select(m => m.RelativeHumidity)
                    .Subscribe(humidityObserver);

                var temperatureObserver = new ObservedValue<Temperature>();
                using IDisposable temperatureSubscription = dependencies
                    .Where(m => m.Measure == Measure.Temperature)
                    .Select(m => m.Temperature)
                    .Subscribe(temperatureObserver);

                while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                {
                    VolatileOrganicCompoundIndex? vocIndex;

                    if (humidityObserver.TryGetValue(out RelativeHumidity relativeHumidity) &&
                        temperatureObserver.TryGetValue(out Temperature temperature))
                    {
                        vocIndex = sensor.ReadVocMeasurement(relativeHumidity, temperature);
                    }
                    else
                    {
                        vocIndex = sensor.ReadVocMeasurement();
                    }

                    if (vocIndex is not null)
                    {
                        measurements.OnNext(Measurement.FromVoc(vocIndex.GetValueOrDefault()));
                    }
                }
            });
    }
}
