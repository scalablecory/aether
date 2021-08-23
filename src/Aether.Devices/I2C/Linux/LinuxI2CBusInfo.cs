using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Aether.Devices.I2C.Linux
{
    internal sealed class LinuxI2CBusInfo : I2CBusInfo
    {
        private readonly string _filePath;

        public override string Name { get; }

        public LinuxI2CBusInfo(string filePath, string name)
        {
            _filePath = filePath;
            Name = name;
        }

        public override string ToString() =>
            Name;

        public override I2CBus Open()
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            return new LinuxI2CBus(_filePath);
        }
    }
}
