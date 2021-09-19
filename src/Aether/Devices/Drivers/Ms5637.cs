using System.Buffers.Binary;
using System.Device.I2c;
using System.Runtime.InteropServices;
using UnitsNet;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// A driver for TE's MS5637.
    /// </summary>
    public sealed class Ms5637 : System.IDisposable
    {
        public const int DefaultI2cAddress = 0x76;

        private readonly I2cDevice _device;
        private readonly ushort _c1, _c2, _c3, _c4, _c5, _c6;

        /// <summary>
        /// Instantiates a new <see cref="MS5637"/>.
        /// </summary>
        /// <param name="device">The I²C device to operate on.</param>
        public Ms5637(I2cDevice device)
        {
            _device = device;

            Reset();

            // TODO: check crc.
            //ushort crc = ReadPROMCoefficient(0xA0);

            _c1 = ReadPROMCoefficient(0xA2);
            _c2 = ReadPROMCoefficient(0xA4);
            _c3 = ReadPROMCoefficient(0xA6);
            _c4 = ReadPROMCoefficient(0xA8);
            _c5 = ReadPROMCoefficient(0xAA);
            _c6 = ReadPROMCoefficient(0xAC);
        }

        /// <inheritdoc/>
        public void Dispose() =>
            _device.Dispose();

        public (Temperature, Pressure) ReadTemperatureAndPressure(OversamplingRatio oversampling = OversamplingRatio.OSR8192)
        {
            (int commandOffset, int delay) = oversampling switch
            {
                OversamplingRatio.OSR256 => (0, 1),
                OversamplingRatio.OSR512 => (2, 2),
                OversamplingRatio.OSR1024 => (4, 3),
                OversamplingRatio.OSR2048 => (6, 5),
                OversamplingRatio.OSR4096 => (8, 9),
                OversamplingRatio.OSR8192 => (10, 17),
                _ => throw new ArgumentOutOfRangeException(nameof(oversampling))
            };

            int d1 = (int)WriteCommandAndReadUInt24((byte)(0x40 + commandOffset), delay);
            int d2 = (int)WriteCommandAndReadUInt24((byte)(0x50 + commandOffset), delay);

            // calc temp.
            int dT = d2 - _c5 * 0x100;
            int temp = 2000 + dT * _c6 / 0x800000;

            // calc temp compensated pressure.
            long off = _c2 * 0x20000L + (long)_c4 * dT / 0x40;
            long sens = _c1 * 0x10000L + (long)_c3 * dT / 0x80;

            // second order compensation for non-linearity.

            long dTsq = (long)dT * dT;

            if (temp < 2000)
            {
                long tempsq = temp - 2000;
                tempsq *= tempsq;

                off -= 61 * tempsq / 0x10;
                sens -= 29 * tempsq / 0x10;

                if (temp < -1500)
                {
                    tempsq = temp + 1500;
                    tempsq *= tempsq;

                    off -= 17 * tempsq;
                    sens -= 9 * tempsq;
                }

                temp -= (int)(3 * dTsq / 0x200000000);
            }
            else
            {
                temp -= (int)(5 * dTsq / 0x4000000000);
            }

            int p = (int)((d1 * sens / 0x200000 - off) / 0x8000);

            return (
                Temperature.FromDegreesCelsius(temp * (1.0 / 100.0)),
                Pressure.FromMillibars(p * (1.0 / 100.0))
                );
        }

        private uint WriteCommandAndReadUInt24(byte command, int delay)
        {
            _device.WriteByte(command);

            Thread.Sleep(delay);

            Span<byte> buffer = stackalloc byte[3];

            command = 0;
            _device.WriteRead(MemoryMarshal.CreateReadOnlySpan(ref command, 1), buffer);

            return ((uint)BinaryPrimitives.ReadUInt16BigEndian(buffer) << 8) | buffer[2];
        }

        private void Reset() =>
            _device.WriteByte(0x1E);

        private ushort ReadPROMCoefficient(byte command)
        {
            Span<byte> buffer = stackalloc byte[2];
            _device.WriteRead(MemoryMarshal.CreateReadOnlySpan(ref command, 1), buffer);

            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        // Bigger number = better accuracy but takes longer to read.
        public enum OversamplingRatio
        {
            OSR256,
            OSR512,
            OSR1024,
            OSR2048,
            OSR4096,
            OSR8192
        }
    }
}
