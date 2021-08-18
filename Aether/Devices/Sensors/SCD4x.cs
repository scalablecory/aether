using Aether.Devices.I2C;
using System.Buffers.Binary;
using System.Reactive.Linq;

namespace Aether.Devices.Sensors
{
    // TODO: calculate CRCs.
    internal sealed class SCD4x : Sensor, II2CSensor
    {
        private readonly I2CDevice _device;
        private readonly SemaphoreSlim _sem = new(initialCount: 1);
        private readonly ObservedValue<float> _pressure;

        public static string Name => "SCD4x";
        public static int DefaultI2CAddress => 0x62;
        public static IEnumerable<SensorDependency> Dependencies { get; } = new[]
        {
            new SensorDependency(Measure.Pressure, required: false)
        };
        public static IEnumerable<MeasureInfo> AvailableMeasures { get; } = new[]
        {
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature),
            new MeasureInfo(Measure.CO2)
        };

        public SCD4x(I2CDevice device, IObservable<Measurement> dependencies)
        {
            _device = device;

            IObservable<float> pressureObservable = dependencies
                .Where(x => x.Measure == Measure.Pressure)
                .Select(x => x.Value);

            _pressure = new ObservedValue<float>(pressureObservable);
        }

        protected override ValueTask DisposeAsyncCore()
        {
            _device.Dispose();
            _sem.Dispose();
            return default;
        }

        public static Sensor CreateFromI2C(I2CDevice device, IObservable<Measurement> dependencies) =>
            new SCD4x(device, dependencies);

        protected override async IAsyncEnumerator<Measurement> GetMeasurementsAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[9];

            // Start periodic measurement.

            BinaryPrimitives.WriteUInt16BigEndian(buffer, 0x21B1);
            await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _device.WriteAsync(buffer.AsMemory(0, 2), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sem.Release();
            }

            // Loop reading measurements.

            long startTicks = 0, endTicks = 5_000;
            long lastPressureCalibrationTicks = Environment.TickCount64;

            while (true)
            {
                // The SCD4x takes a measurement every 5 seconds.

                long delay = endTicks - startTicks;

                if (delay > 0)
                {
                    try
                    {
                        await Task.Delay((int)Math.Max(delay, 5_000), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
                    {
                        break;
                    }
                }

                startTicks = Environment.TickCount64;

                // Start reading the measurement

                BinaryPrimitives.WriteUInt16BigEndian(buffer, 0xEC05);

                try
                {
                    await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
                {
                    break;
                }

                try
                {
                    await _device.WriteAsync(buffer.AsMemory(0, 2), cancellationToken);

                    // Wait 2ms for device to have a response.

                    try
                    {
                        await Task.Delay(2, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
                    {
                        break;
                    }

                    // Finish reading the measurement.

                    await _device.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _sem.Release();
                }

                float co2 = BinaryPrimitives.ReadUInt16BigEndian(buffer);
                yield return new Measurement(Measure.CO2, co2);

                float humidity = (float)(BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(6, 2)) * (100.0 / 65535.0));
                yield return new Measurement(Measure.Humidity, humidity);

                float temperature = (float)(BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(3, 2)) * (35.0 / 13107.0) - 45.0);
                yield return new Measurement(Measure.Temperature, temperature);

                endTicks = Environment.TickCount64;

                if ((endTicks - lastPressureCalibrationTicks) >= 10_000 && _pressure.TryGetValueIfChanged(out float pressure))
                {
                    // Update pressure calibration every 10 seconds, if available.

                    lastPressureCalibrationTicks = endTicks;

                    BinaryPrimitives.WriteUInt16BigEndian(buffer, 0xE000);
                    BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), (ushort)(pressure * (1.0f / 100.0f)));
                    buffer[4] = 0; // TODO: calculate CRC.

                    try
                    {
                        await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
                    {
                        break;
                    }

                    try
                    {
                        await _device.WriteAsync(buffer.AsMemory(0, 5), cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        _sem.Release();
                    }

                    endTicks = Environment.TickCount64;
                }
            }

            // Stop periodic measurement.

            BinaryPrimitives.WriteUInt16BigEndian(buffer, 0x3F86);

            await _sem.WaitAsync().ConfigureAwait(false);
            try
            {
                await _device.WriteAsync(buffer.AsMemory(0, 2), cancellationToken: default).ConfigureAwait(false);
            }
            finally
            {
                _sem.Release();
            }
        }
    }
}
