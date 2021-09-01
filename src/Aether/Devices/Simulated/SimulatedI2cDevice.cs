using System.Device.I2c;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aether.Devices.Simulated
{
    internal abstract class SimulatedI2cDevice : I2cDevice
    {
        public sealed override I2cConnectionSettings ConnectionSettings { get; }

        public SimulatedI2cDevice(int deviceAddress)
        {
            ConnectionSettings = new I2cConnectionSettings(0, deviceAddress);
        }

        public sealed override byte ReadByte()
        {
            Unsafe.SkipInit(out byte b);
            Read(MemoryMarshal.CreateSpan(ref b, 1));
            return b;
        }

        public sealed override void WriteByte(byte value)
        {
            Write(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
        }

        public sealed override void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
        {
            Write(writeBuffer);
            Read(readBuffer);
        }
    }
}
