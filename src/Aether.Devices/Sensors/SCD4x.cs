using Aether.Devices.I2C;
using System.Buffers;
using System.Buffers.Binary;

namespace Aether.Devices.Sensors
{
    /// <summary>
    /// A driver for Sensirion's SCD4x.
    /// </summary>
    public sealed class SCD4x : IDisposable
    {
        private static readonly byte[] s_startPeriodicMeasurementBytes = new byte[] { 0x21, 0xB1 };
        private static readonly byte[] s_readPeriodicMeasurementBytes = new byte[] { 0xEC, 0x05 };
        private static readonly byte[] s_stopPeriodicMeasurementBytes = new byte[] { 0x3F, 0x86 };
        private readonly I2CDevice _device;
        private readonly SemaphoreSlim _sem = new(initialCount: 1);

        /// <summary>
        /// Instantiates a new <see cref="SCD4x"/>.
        /// </summary>
        /// <param name="device">The I²C device to operate on.</param>
        public SCD4x(I2CDevice device)
        {
            _device = device;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _device.Dispose();
            _sem.Dispose();
        }

        /// <summary>
        /// Calibrates the sensor to operate at a specific barometric pressure.
        /// Doing so will make measurements more accurate.
        /// </summary>
        /// <param name="pressure">The barometric pressure to use when calibrating the sensor.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>for the operation.</param>
        public async ValueTask SetPressureCalibrationAsync(double pressure, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[5];

            BinaryPrimitives.WriteUInt16BigEndian(buffer, 0xE000);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), (ushort)(pressure * (1.0 / 100.0)));
            buffer[4] = SHT4x.CRC8(buffer.AsSpan(0, 4));

            await WriteCommandAsync(buffer, cancellationToken).ConfigureAwait(false);
            await Task.Delay(1).ConfigureAwait(false);
        }

        /// <summary>
        /// Instructs the sensor to start performing periodic measurements.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>for the operation.</param>
        public ValueTask StartPeriodicMeasurementsAsync(CancellationToken cancellationToken = default) =>
            WriteCommandAsync(s_startPeriodicMeasurementBytes, cancellationToken);

        /// <summary>
        /// Reads a periodic CO₂, humidity, and temperature measurement from the sensor.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>for the operation.</param>
        /// <returns>A tuple of CO₂, humidity, and temperature.</returns>
        public async ValueTask<(float co2, float humidity, float temperature)> ReadPeriodicMeasurementAsync(CancellationToken cancellationToken = default)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(9);

            await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _device.WriteAsync(s_readPeriodicMeasurementBytes, cancellationToken).ConfigureAwait(false);

                await Task.Delay(2, cancellationToken).ConfigureAwait(false);

                await _device.ReadAsync(buffer.AsMemory(0, 9), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sem.Release();
            }

            _ = buffer[8];
            float co2 = SHT4x.ReadUInt16(buffer.AsSpan(0, 2), buffer[2]);
            float temperature = (float)(SHT4x.ReadUInt16(buffer.AsSpan(3, 2), buffer[5]) * (35.0 / 13107.0) - 45.0);
            float humidity = (float)(SHT4x.ReadUInt16(buffer.AsSpan(6, 2), buffer[8]) * (100.0 / 65535.0));

            ArrayPool<byte>.Shared.Return(buffer);

            return (co2, humidity, temperature);
        }

        /// <summary>
        /// Instructs the sensor to stop performing periodic measurements.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>for the operation.</param>
        public async ValueTask StopPeriodicMeasurementsAsync(CancellationToken cancellationToken = default)
        {
            await WriteCommandAsync(s_stopPeriodicMeasurementBytes, cancellationToken).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
        }

        private async ValueTask WriteCommandAsync(byte[] commandBytes, CancellationToken cancellationToken)
        {
            await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _device.WriteAsync(commandBytes, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sem.Release();
            }
        }
    }
}
