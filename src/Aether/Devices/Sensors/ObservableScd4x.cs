using System.Device.I2c;
using System.Reactive.Linq;
using Aether.Devices.Sensors.Metadata;
using Aether.Reactive;
using Iot.Device.Scd4x;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal sealed class ObservableScd4x : I2cSensorFactory
    {
        public static ObservableScd4x Instance { get; } = new ObservableScd4x();

        public override int DefaultAddress => Scd4x.DefaultI2cAddress;

        public override string Manufacturer => "Sensirion";

        public override string Name => "SCD4x";

        public override string Uri => "https://www.sensirion.com/en/environmental-sensors/carbon-dioxide-sensors/carbon-dioxide-sensor-scd4x/";

        public override IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.CO2),
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public override IEnumerable<SensorDependency> Dependencies => new[]
        {
            new SensorDependency(Measure.BarometricPressure, required: false)
        };

        // TODO: self-calibration command.
        public override IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public override IObservable<Measurement> OpenSensor(Func<I2cDevice> deviceFunc, IObservable<Measurement> dependencies) =>
            Observable.Create(async (IObserver<Measurement> measurements, CancellationToken cancellationToken) =>
            {
                using I2cDevice device = deviceFunc();
                using var sensor = new Scd4x(device);
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
                using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

                var pressureObserver = new ObservedValue<Pressure>();
                using IDisposable pressureSubscription = dependencies
                    .Where(m => m.Measure == Measure.BarometricPressure)
                    .Select(m => m.BarometricPressure)
                    .Subscribe(pressureObserver);

                sensor.StartPeriodicMeasurements();
                try
                {
                    while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                    {
                        (VolumeConcentration? co2, RelativeHumidity? humidity, Temperature? temperature) =
                            await sensor.ReadPeriodicMeasurementAsync().ConfigureAwait(false);

                        if (co2 is not null) measurements.OnNext(Measurement.FromCo2(co2.GetValueOrDefault()));
                        if (humidity is not null) measurements.OnNext(Measurement.FromRelativeHumidity(humidity.GetValueOrDefault()));
                        if (temperature is not null) measurements.OnNext(Measurement.FromTemperature(temperature.GetValueOrDefault()));

                        if (pressureObserver.TryGetValueIfChanged(out Pressure pressure))
                        {
                            sensor.SetPressureCalibration(pressure);
                        }
                    }
                }
                finally
                {
                    sensor.StopPeriodicMeasurements();
                }
            });
    }
}
