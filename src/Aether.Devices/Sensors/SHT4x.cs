using Aether.Devices.I2C;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Aether.Devices.Sensors
{
    /// <summary>
    /// A driver for Sensirion's SHT4x.
    /// </summary>
    public sealed class SHT4x : IDisposable
    {
        private readonly I2CDevice _device;
        private readonly SemaphoreSlim _sem = new(initialCount: 1);
        private readonly byte[] _buffer = new byte[6];

        /// <summary>
        /// Instantiates a new <see cref="SHT4x"/>.
        /// </summary>
        /// <param name="device">The I²C device to operate on.</param>
        public SHT4x(I2CDevice device)
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
        /// Performs a highly repeatable measurement of humidity and temperature.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation.</param>
        /// <returns>A tuple of humidity and temperature.</returns>
        public async ValueTask<(float humidity, float temperature)> ReadHighlyRepeatableMeasurementAsync(CancellationToken cancellationToken = default)
        {
            float humidity;
            float temperature;

            await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _buffer[0] = 0xFD;

                // Start the measurement read.
                await _device.WriteAsync(_buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);

                // A high repeatability read takes 8.2ms to complete.
                // TODO: detect a NACKed address byte and delay even more.
                await Task.Delay(9, cancellationToken).ConfigureAwait(false);

                // Finish the read.
                Debug.Assert(_buffer.Length == 6);
                await _device.ReadAsync(_buffer, cancellationToken).ConfigureAwait(false);

                _ = _buffer[5];
                humidity = (float)Math.Clamp(ReadUInt16(_buffer.AsSpan(0, 2), _buffer[2]) * (1.0 / 52428.0) - (3.0 / 50.0), 0.0, 1.0);
                temperature = (float)(ReadUInt16(_buffer.AsSpan(2, 2), _buffer[5]) * (35.0 / 13107.0) - 45.0);
            }
            finally
            {
                _sem.Release();
            }

            return (humidity, temperature);
        }

        internal static ushort ReadUInt16(ReadOnlySpan<byte> bytes, byte crc)
        {
            CheckCRC8(bytes, crc);
            return BinaryPrimitives.ReadUInt16BigEndian(bytes);
        }

        internal static void CheckCRC8(ReadOnlySpan<byte> bytes, byte check)
        {
            if (CRC8(bytes) != check) ThrowIntegrityException();

            static void ThrowIntegrityException() =>
                throw new Exception("Integrity check failed; invalid CRC8.");
        }

        internal static byte CRC8(ReadOnlySpan<byte> bytes)
        {
            uint crc = 0xFF;

            foreach (byte b in bytes)
            {
                crc ^= b;

                int bits = 8;
                do
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc = (crc << 1) ^ 0x31;
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
                while (--bits != 0);
            }

            return (byte)crc;
        }
    }
}
