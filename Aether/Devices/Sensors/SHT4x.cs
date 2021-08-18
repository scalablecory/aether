using Aether.Devices.I2C;
using System.Buffers.Binary;
using System.Reactive.Linq;

namespace Aether.Devices.Sensors
{
    internal sealed class SHT4x : Sensor, II2CSensor
    {
        private readonly I2CDevice _device;
        private readonly SemaphoreSlim _sem = new(initialCount: 1);

        static string ISensor.Name => "SHT4x";
        static int II2CSensor.DefaultI2CAddress => 0x44;
        static IEnumerable<SensorDependency> ISensor.Dependencies => Array.Empty<SensorDependency>();
        static IEnumerable<MeasureInfo> ISensor.AvailableMeasures { get; } = new[]
        {
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature),
        };

        private SHT4x(I2CDevice device)
        {
            _device = device;
        }

        protected override ValueTask DisposeAsyncCore()
        {
            _sem.Dispose();
            _device.Dispose();
            return default;
        }

        public static Sensor CreateFromI2C(I2CDevice device, IObservable<Measurement> dependencies) =>
            new SHT4x(device);

        protected override async IAsyncEnumerator<Measurement> GetMeasurementsAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[6];

            while (true)
            {
                int startTickCount = Environment.TickCount;

                float humidity, temperature;

                await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    // Start the measurement read.

                    buffer[0] = 0xFD;
                    await _device.WriteAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);

                    // A high repeatability read takes at least 8.2ms to complete.
                    // TODO: detect a NACKed address byte and delay even more.
                    await Task.Delay(9, cancellationToken).ConfigureAwait(false);

                    // Finish the read.

                    await _device.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                    // TODO: check CRC values.
                    humidity = (float)Math.Clamp(BinaryPrimitives.ReadUInt16BigEndian(buffer) * (1.0 / 52428.0) - (3.0 / 50.0), 0.0, 1.0);
                    temperature = (float)(BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(2)) * (35.0 / 13107.0) - 45.0);

                }
                finally
                {
                    _sem.Release();
                }

                yield return new Measurement(Measure.Humidity, humidity);
                yield return new Measurement(Measure.Temperature, temperature);

                int endTickCount = Environment.TickCount;
                int delay = 1_000 - (endTickCount - startTickCount);

                if (delay > 0)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

    }
}
