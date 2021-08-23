using Aether.Devices.I2C;
using Aether.Devices.Sensors;
using Aether.Reactive;
using System.Reactive.Linq;

namespace Aether.Devices.Metadata.Sensors
{
    internal class SCD4xSensorFactory : I2CSensorFactory
    {
        public override int DefaultAddress => 0x62;

        public override string Manufacturer => "Sensirion";

        public override string Name => "SCD4x";

        public override string Uri => "https://www.sensirion.com/en/environmental-sensors/carbon-dioxide-sensors/carbon-dioxide-sensor-scd4x/";

        public override IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.CO2),
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public override IObservable<Measurement> OpenDevice(I2CDevice device, IObservable<Measurement> dependencies) =>
            Observable.Using(() => new SCD4x(device),
                sensor => Observable.Create<Measurement>(async (observer, cancellationToken) =>
                {
                    await sensor.StartPeriodicMeasurementsAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5_000));
                        using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

                        var pressureObserver = new ObservedValue<float>();
                        using IDisposable subscription = dependencies
                            .Where(static measurement => measurement.Measure == Measure.Pressure)
                            .Select(static measurement => measurement.Value)
                            .Subscribe(pressureObserver);

                        while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                        {
                            (float co2, float humidity, float temperature) =
                                await sensor.ReadPeriodicMeasurementAsync(cancellationToken).ConfigureAwait(false);

                            observer.OnNext(new Measurement(Measure.CO2, co2));
                            observer.OnNext(new Measurement(Measure.Humidity, humidity));
                            observer.OnNext(new Measurement(Measure.Temperature, temperature));

                            if (pressureObserver.TryGetValueIfChanged(out float pressure))
                            {
                                await sensor.SetPressureCalibrationAsync(pressure, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    finally
                    {
                        // Intentionally not observing a cancellation token here, to ensure measurement stops.
                        await sensor.StopPeriodicMeasurementsAsync().ConfigureAwait(false);
                    }
                }));
    }
}
