using Aether.Devices.Sensors.Metadata;
using Aether.Reactive;
using System.Device.I2c;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UnitsNet;

namespace Aether.Devices.Sensors.Observable
{
    internal class ObservableScd4x : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Drivers.Scd4x _sensor;
        private readonly ReplaySubject<VolumeConcentration> _co2 = new(bufferSize: 1);
        private readonly ReplaySubject<RelativeHumidity> _rh = new(bufferSize: 1);
        private readonly ReplaySubject<Temperature> _t = new(bufferSize: 1);
        private readonly IEnumerable<ObservableSensor> _dependencies;

        public override IObservable<VolumeConcentration> CO2 => _co2;
        public override IObservable<RelativeHumidity> RelativeHumidity => _rh;
        public override IObservable<Temperature> Temperature => _t;

        private ObservableScd4x(I2cDevice device, IEnumerable<ObservableSensor> dependencies)
        {
            _sensor = new Drivers.Scd4x(device);
            _dependencies = dependencies;
        }

        protected override void DisposeCore()
        {
            _sensor.Dispose();
            _co2.Dispose();
            _rh.Dispose();
            _t.Dispose();
        }

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            var pressureObserver = new ObservedValue<Pressure>();
            using IDisposable subscription = _dependencies
                .Select(dependency => dependency.BarometricPressure)
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

                    (VolumeConcentration? co2, RelativeHumidity? humidity, Temperature? temperature) =
                        _sensor.ReadPeriodicMeasurement();

                    if (co2 is not null) _co2.OnNext(co2.GetValueOrDefault());
                    if (humidity is not null) _rh.OnNext(humidity.GetValueOrDefault());
                    if (temperature is not null) _t.OnNext(temperature.GetValueOrDefault());

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

        protected override void OnError(Exception ex)
        {
            _co2.OnError(ex);
            _rh.OnError(ex);
            _t.OnError(ex);
        }

        protected override void OnCompleted()
        {
            _co2.OnCompleted();
            _rh.OnCompleted();
            _t.OnCompleted();
        }

        #region IObservableI2CSensorFactory

        public static int DefaultAddress => Drivers.Scd4x.DefaultI2cAddress;

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
            new ObservableScd4x(device, dependencies);

        #endregion
    }
}
