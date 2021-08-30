using System.Buffers.Binary;
using System.Device.I2c;
using System.Runtime.InteropServices;
using UnitsNet;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// A driver for TE's MS5637.
    /// </summary>
    public sealed class MS5637 : IDisposable
    {
        private readonly I2cDevice _device;
        private ushort _c1, _c2, _c3, _c4, _c5, _c6;

        /// <summary>
        /// Instantiates a new <see cref="MS5637"/>.
        /// </summary>
        /// <param name="device">The I²C device to operate on.</param>
        public MS5637(I2cDevice device)
        {
            _device = device;
        }

        /// <inheritdoc/>
        public void Dispose() =>
            _device.Dispose();

        public (Temperature, Pressure) ReadTemperatureAndPressure()
        {
            int d1 = WriteCommandAndReadUInt24(0x4A);
            int d2 = WriteCommandAndReadUInt24(0x5A);

            // calc temp.
            int dT = d2 - _c5 * 0x100;
            int temp = 2000 + dT * _c6 / 0x800000;

            // calc temp compensated pressure.
            long off = _c2 * 0x20000L + (long)_c4 * dT / 0x40;
            long sens = _c1 * 0x10000L + (long)_c3 * dT / 0x80;

            // second order compensation for non-linearity.

            int t2;
            if (temp < 2000)
            {
                int tempsq = temp - 2000;
                tempsq *= tempsq;

                t2 = (int)(3L * dT * dT / 0x200000000);
                off -= 61 * tempsq / 0x10;
                sens -= 29 * tempsq / 0x10;

                if (temp < -1500)
                {
                    tempsq = temp + 1500;
                    tempsq *= tempsq;

                    off -= 17 * tempsq;
                    sens -= 9 * tempsq;
                }
            }
            else
            {
                t2 = (int)(5L * dT * dT / 0x4000000000);
            }

            temp -= t2;

            int p = (int)((d1 * sens / 0x200000 - off) / 0x8000);

            return (
                Temperature.FromDegreesCelsius(temp * (1.0 / 10.0)),
                Pressure.FromMillibars(p * (1.0 / 10.0))
                );
        }

        private int WriteCommandAndReadUInt24(byte command)
        {
            _device.WriteByte(command);

            Thread.Sleep(17);

            Span<byte> buffer = stackalloc byte[3];

            command = 0;
            _device.WriteRead(MemoryMarshal.CreateReadOnlySpan(ref command, 1), buffer);

            return (int)(((uint)BinaryPrimitives.ReadUInt16BigEndian(buffer) << 8) | buffer[2]);
        }

        private void Initialize()
        {
            Reset();

            _c1 = ReadPROMCoefficient(0xA0);
            _c2 = ReadPROMCoefficient(0xA2);
            _c3 = ReadPROMCoefficient(0xA4);
            _c4 = ReadPROMCoefficient(0xA6);
            _c5 = ReadPROMCoefficient(0xA8);
            _c6 = ReadPROMCoefficient(0xAA);

            ushort crc = ReadPROMCoefficient(0xAC);
            // TODO: use CRC.
        }

        private void Reset() =>
            _device.WriteByte(0x1E);

        private ushort ReadPROMCoefficient(byte command)
        {
            Span<byte> buffer = stackalloc byte[2];
            _device.WriteRead(MemoryMarshal.CreateReadOnlySpan(ref command, 1), buffer);

            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }
    }
}
