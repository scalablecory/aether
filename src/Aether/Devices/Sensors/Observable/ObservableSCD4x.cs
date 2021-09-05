using Aether.Devices.Sensors.Metadata;
using Aether.Reactive;
using System.Device.I2c;
using System.Reactive.Linq;
using UnitsNet;

namespace Aether.Devices.Sensors.Observable
{
    internal class ObservableSCD4x : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Drivers.SCD4x _sensor;
        private readonly IEnumerable<ObservableSensor> _dependencies;

        private ObservableSCD4x(I2cDevice device, IEnumerable<ObservableSensor> dependencies)
            : base(Measure.CO2, Measure.Humidity, Measure.Temperature)
        {
            _sensor = new Drivers.SCD4x(device);
            _dependencies = dependencies;
        }

        protected override void DisposeCore() =>
            _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5_000));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            var pressureObserver = new ObservedValue<Pressure>();
            using IDisposable subscription = _dependencies
                .Select(static dependency => dependency.BarometricPressure)
                .Merge()
                .Subscribe(pressureObserver);

            _sensor.StartPeriodicMeasurements();
            try
            {
                while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                {
                    while (!_sensor.CheckDataReady())
                    {
                        await Task.Delay(500).ConfigureAwait(false);
                    }

                    (VolumeConcentration co2, RelativeHumidity humidity, Temperature temperature) =
                        _sensor.ReadPeriodicMeasurement();

                    OnNextCO2(co2);
                    OnNextRelativeHumidity(humidity);
                    OnNextTemperature(temperature);

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

        public static int DefaultAddress => Drivers.SCD4x.DefaultAddress;

        public static string Manufacturer => "Sensirion";

        public static string Name => "SCD4x";

        public static string Uri => "https://www.sensirion.com/en/environmental-sensors/carbon-dioxide-sensors/carbon-dioxide-sensor-scd4x/";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.CO2),
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public static IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;

        // TODO: self-calibration command.
        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenSensor(I2cDevice device, IEnumerable<ObservableSensor> dependencies) =>
            new ObservableSCD4x(device, dependencies);

        #endregion
    }
}
