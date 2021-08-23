using Aether.Devices.I2C;
using Aether.Devices.Sensors;
using System.Reactive.Linq;

namespace Aether.Devices.Metadata.Sensors
{
    internal sealed class SHT4xSensorFactory : I2CSensorFactory
    {
        public override int DefaultAddress => 0x44;

        public override string Manufacturer => "Sensirion";

        public override string Name => "SHT4x";

        public override string Uri => "https://www.sensirion.com/en/environmental-sensors/humidity-sensors/humidity-sensor-sht4x/";

        public override IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public override IObservable<Measurement> OpenDevice(I2CDevice device, IObservable<Measurement> dependencies) =>
            Observable.Using(() => new SHT4x(device),
                sensor => Observable.Create<Measurement>(async (observer, cancellationToken) =>
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5_000));
                    using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

                    while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                    {
                        (float humidity, float temperature) =
                            await sensor.ReadHighlyRepeatableMeasurementAsync(cancellationToken).ConfigureAwait(false);

                        observer.OnNext(new Measurement(Measure.Humidity, humidity));
                        observer.OnNext(new Measurement(Measure.Temperature, temperature));
                    }
                }));
    }
}
