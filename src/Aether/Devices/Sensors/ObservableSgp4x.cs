using Aether.CustomUnits;
using Aether.Devices.Sensors.Metadata;
using Aether.Reactive;
using System.Device.I2c;
using System.Reactive.Linq;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal class ObservableSgp4x : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Drivers.Sgp4x _sensor;
        private readonly IObservable<Measurement> _dependencies;

        private ObservableSgp4x(I2cDevice device, IObservable<Measurement> dependencies)
        {
            _sensor = new Drivers.Sgp4x(device);
            _dependencies = dependencies;
            Start();
        }

        public static int DefaultAddress => Drivers.Sgp4x.DefaultI2cAddress;

        public static string Manufacturer => "Sensirion";

        public static string Name => "SGP4x";

        public static string Uri => "https://www.sensirion.com/en/environmental-sensors/gas-sensors/sgp40/";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.VOC),
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public static IEnumerable<SensorDependency> Dependencies => new[]
        {
            new SensorDependency(Measure.Temperature, required: false),
            new SensorDependency(Measure.Humidity, required: false)
        };

        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenSensor(I2cDevice device, IObservable<Measurement> dependencies) => new ObservableSgp4x(device, dependencies);

        protected override void DisposeCore() => _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            var humidityObserver = new ObservedValue<RelativeHumidity>();
            using IDisposable humididtySubscription = _dependencies
                .Where(m => m.Measure == Measure.Humidity)
                .Select(m => m.RelativeHumidity)
                .Subscribe(humidityObserver);

            var temperatureObserver = new ObservedValue<Temperature>();
            using IDisposable temperatureSubscription = _dependencies
                .Where(m => m.Measure == Measure.Temperature)
                .Select(m => m.Temperature)
                .Subscribe(temperatureObserver);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                VolatileOrganicCompoundIndex? vocIndex = null;

                if (humidityObserver.TryGetValueIfChanged(out RelativeHumidity relativeHumidity) &&
                    temperatureObserver.TryGetValueIfChanged(out Temperature temperature))
                {
                    vocIndex = _sensor.ReadVocMeasurement(relativeHumidity, temperature);
                }
                else
                {
                    vocIndex = _sensor.ReadVocMeasurement();
                }
                

                

                if (vocIndex is not null) OnNextVolitileOrganicCompound(vocIndex.Value);
            }
        }
    }
}
