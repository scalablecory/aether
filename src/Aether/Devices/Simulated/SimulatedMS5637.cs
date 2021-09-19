using System.Buffers.Binary;

namespace Aether.Devices.Simulated
{
    internal sealed class SimulatedMS5637 : SimulatedI2cDevice
    {
        private byte? _currentCommand;
        private uint _readData;

        public SimulatedMS5637()
            : base(Drivers.Ms5637.DefaultI2cAddress)
        {
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length == 1)
            {
                switch (buffer[0])
                {
                    // D1 (pressure).
                    case 0x40:
                    case 0x42:
                    case 0x44:
                    case 0x46:
                    case 0x48:
                    case 0x4A:
                        _readData = 6465444;
                        _currentCommand = null;
                        break;
                    // D2 (temperature).
                    case 0x50:
                    case 0x52:
                    case 0x54:
                    case 0x56:
                    case 0x58:
                    case 0x5A:
                        _readData = 8077636;
                        _currentCommand = null;
                        break;
                    default:
                        _currentCommand = buffer[0];
                        break;
                }
            }
            else if (buffer.Length > 1)
            {
                throw new IOException("Invalid command; unexpected bytes after command byte.");
            }
        }

        public override void Read(Span<byte> buffer)
        {
            switch (_currentCommand)
            {
                // Read current measurement.
                case 0x00:
                    if (buffer.Length != 3)
                    {
                        throw new IOException("The read operation failed; expected a read of 3 bytes.");
                    }
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)((_readData >> 8) & 0xFFFF));
                    buffer[2] = (byte)(_readData & 0xFF);
                    break;
                // PROM crc & coefficients.
                case 0xA0:
                    if (buffer.Length != 2) ThrowNot16Bits();
                    // TODO: actual CRC.
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, 0);
                    break;
                case 0xA2:
                    if (buffer.Length != 2) ThrowNot16Bits();
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, 46372);
                    break;
                case 0xA4:
                    if (buffer.Length != 2) ThrowNot16Bits();
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, 43981);
                    break;
                case 0xA6:
                    if (buffer.Length != 2) ThrowNot16Bits();
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, 29059);
                    break;
                case 0xA8:
                    if (buffer.Length != 2) ThrowNot16Bits();
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, 27842);
                    break;
                case 0xAA:
                    if (buffer.Length != 2) ThrowNot16Bits();
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, 31553);
                    break;
                case 0xAC:
                    if (buffer.Length != 2) ThrowNot16Bits();
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, 28165);
                    break;
                case null:
                    throw new IOException("No command has been written.");
                default:
                    throw new IOException($"Unknown command 0x{_currentCommand:X2}.");
            }

            static void ThrowNot16Bits() =>
                throw new IOException("The read operation failed; expected a read of 2 bytes.");
        }
    }
}
