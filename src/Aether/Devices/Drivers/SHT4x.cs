using System.Buffers.Binary;
using System.Device.I2c;
using UnitsNet;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// A driver for Sensirion's SHT4x.
    /// </summary>
    public sealed class SHT4x : IDisposable
    {
        private static ReadOnlySpan<byte> HighlyRepeatableReadBytes => new byte[] { 0xFD };

        private readonly I2cDevice _device;

        /// <summary>
        /// Instantiates a new <see cref="SHT4x"/>.
        /// </summary>
        /// <param name="device">The I²C device to operate on.</param>
        public SHT4x(I2cDevice device)
        {
            _device = device;
        }

        /// <inheritdoc/>
        public void Dispose() =>
            _device.Dispose();

        /// <summary>
        /// Performs a highly repeatable measurement of humidity and temperature.
        /// </summary>
        /// <returns>A tuple of humidity and temperature.</returns>
        public (RelativeHumidity, Temperature) ReadHighlyRepeatableMeasurement()
        {
            _device.Write(HighlyRepeatableReadBytes);
            Thread.Sleep(9);

            Span<byte> buffer = stackalloc byte[6];
            _device.Read(buffer);

            _ = buffer[5];
            ushort deviceHumidity = ReadUInt16(buffer[..2], buffer[2]);
            ushort deviceTemperature = ReadUInt16(buffer[2..4], buffer[5]);

            RelativeHumidity h = RelativeHumidity.FromPercent(Math.Clamp(Math.FusedMultiplyAdd(deviceHumidity, 1.0 / 52428.0, -3.0 / 50.0), 0.0, 1.0));
            Temperature t = Temperature.FromDegreesCelsius(Math.FusedMultiplyAdd(deviceTemperature, 35.0 / 13107.0, -45.0));

            return (h, t);
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
