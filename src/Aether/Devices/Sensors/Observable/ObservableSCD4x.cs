using Aether.Devices.I2C;
using Aether.Devices.Sensors.Metadata;
using Aether.Reactive;
using System.Reactive.Linq;

namespace Aether.Devices.Sensors.Observable
{
    internal class ObservableSCD4x : ObservableSensor, IObservableI2CSensorFactory
    {
        private readonly SCD4x _sensor;
        private readonly IObservable<Measurement> _dependencies;

        public static int DefaultAddress => 0x62;

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

        public static ObservableSensor OpenDevice(I2CDevice device, IObservable<Measurement> dependencies) =>
            new ObservableSCD4x(device, dependencies);

        private ObservableSCD4x(I2C.I2CDevice device, IObservable<Measurement> dependencies)
        {
            _sensor = new SCD4x(device);
            _dependencies = dependencies;
        }

        protected override void DisposeCore() =>
            _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            await _sensor.StartPeriodicMeasurementsAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5_000));
                using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

                var pressureObserver = new ObservedValue<float>();
                using IDisposable subscription = _dependencies
                    .Where(static measurement => measurement.Measure == Measure.Pressure)
                    .Select(static measurement => measurement.Value)
                    .Subscribe(pressureObserver);

                while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                {
                    (float co2, float humidity, float temperature) =
                        await _sensor.ReadPeriodicMeasurementAsync(cancellationToken).ConfigureAwait(false);

                    OnNext(Measure.CO2, co2);
                    OnNext(Measure.Humidity, humidity);
                    OnNext(Measure.Temperature, temperature);

                    if (pressureObserver.TryGetValueIfChanged(out float pressure))
                    {
                        await _sensor.SetPressureCalibrationAsync(pressure, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                // Intentionally not observing a cancellation token here, to ensure measurement stops.
                await _sensor.StopPeriodicMeasurementsAsync().ConfigureAwait(false);
            }
        }
    }
}
