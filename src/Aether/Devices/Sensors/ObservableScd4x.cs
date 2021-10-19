using Aether.Devices.Sensors.Metadata;
using Aether.Reactive;
using Iot.Device.Scd4x;
using System.Device.I2c;
using System.Reactive.Linq;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal class ObservableScd4x : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Scd4x _sensor;
        private readonly IObservable<Measurement> _dependencies;

        private ObservableScd4x(I2cDevice device, IObservable<Measurement> dependencies)
        {
            _sensor = new Scd4x(device);
            _dependencies = dependencies;
            Start();
        }

        protected override void DisposeCore() =>
            _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            var pressureObserver = new ObservedValue<Pressure>();
            using IDisposable pressureSubscription = _dependencies
                .Where(m => m.Measure == Measure.BarometricPressure)
                .Select(m => m.BarometricPressure)
                .Subscribe(pressureObserver);

            _sensor.StartPeriodicMeasurements();
            try
            {
                while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                {
                    (VolumeConcentration? co2, RelativeHumidity? humidity, Temperature? temperature) =
                        await _sensor.ReadPeriodicMeasurementAsync().ConfigureAwait(false);

                    if (co2 is not null) OnNextCo2(co2.GetValueOrDefault());
                    if (humidity is not null) OnNextRelativeHumidity(humidity.GetValueOrDefault());
                    if (temperature is not null) OnNextTemperature(temperature.GetValueOrDefault());

                    if (pressureObserver.TryGetValueIfChanged(out Pressure pressure))
                    {
                        _sensor.SetPressureCalibration(pressure);
                    }
                }
            }
            finally
            {
                _sensor.StopPeriodicMeasurements();
            }
        }

        #region IObservableI2CSensorFactory

        public static int DefaultAddress => Scd4x.DefaultI2cAddress;

        public static string Manufacturer => "Sensirion";

        public static string Name => "SCD4x";

        public static string Uri => "https://www.sensirion.com/en/environmental-sensors/carbon-dioxide-sensors/carbon-dioxide-sensor-scd4x/";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.CO2),
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public static IEnumerable<SensorDependency> Dependencies => new[]
        {
            new SensorDependency(Measure.BarometricPressure, required: false)
        };

        // TODO: self-calibration command.
        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenSensor(I2cDevice device, IObservable<Measurement> dependencies) =>
            new ObservableScd4x(device, dependencies);

        #endregion
    }
}
