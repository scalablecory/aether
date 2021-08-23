using System.Runtime.Versioning;

namespace Aether.Devices.I2C.Linux
{
    /// <summary>
    /// An I²C driver for Linux, via SMBus APIs.
    /// </summary>
    [SupportedOSPlatform("linux")]
    public sealed class LinuxI2CDriver : I2CDriver
    {
        /// <summary>
        /// A singleton instance of the <see cref="LinuxI2CDriver"/>.
        /// </summary>
        public static LinuxI2CDriver Instance { get; } = new LinuxI2CDriver();

        private LinuxI2CDriver()
        {
        }

        /// <inheritdoc/>
        public override IEnumerable<I2CBusInfo> EnumerateBusses()
        {
            foreach (string filePath in Directory.EnumerateFiles("/sys/class/i2c-dev/", "i2c-*"))
            {
                string name = File.ReadAllText(Path.Combine(filePath, "name")).Trim();
                yield return new LinuxI2CBusInfo(Path.Combine("/dev/", Path.GetFileName(filePath)), name);
            }
        }
    }
}
