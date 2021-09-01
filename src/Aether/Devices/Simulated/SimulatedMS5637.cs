using System.Buffers.Binary;

namespace Aether.Devices.Simulated
{
    internal sealed class SimulatedMS5637 : SimulatedI2cDevice
    {
        private byte? _currentCommand;

        public SimulatedMS5637()
            : base(Drivers.MS5637.DefaultAddress)
        {
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length == 1)
            {
                _currentCommand = buffer[0];
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

                // D1 (pressure).
                case 0x40:
                case 0x42:
                case 0x44:
                case 0x46:
                case 0x48:
                case 0x4A:
                    if (buffer.Length != 3) ThrowNot24Bits();

                    const uint D1 = 6465444;
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)(D1 >> 8));
                    buffer[2] = (byte)(D1 & 0xFF);
                    break;

                // D2 (temperature).
                case 0x50:
                case 0x52:
                case 0x54:
                case 0x56:
                case 0x58:
                case 0x5A:
                    if (buffer.Length != 3) ThrowNot24Bits();
                    
                    const uint D2 = 8077636;
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)(D2 >> 8));
                    buffer[2] = (byte)(D2 & 0xFF);
                    break;
                default:
                    throw new IOException("Unknown command.");
            }

            static void ThrowNot16Bits() =>
                throw new IOException("The read operation failed; expected a read of 2 bytes.");

            static void ThrowNot24Bits() =>
                throw new IOException("The read operation failed; expected a read of 3 bytes.");
        }
    }
}
