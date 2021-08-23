using System.Runtime.Versioning;
using System.Text;

namespace Aether.Devices.I2C.Linux
{
    /// <summary>
    /// An I²C bus for Linux, via SMBus APIs.
    /// </summary>
    [SupportedOSPlatform("linux")]
    public sealed class LinuxI2CBus : I2CBus
    {
        private readonly SemaphoreSlim _sem = new(initialCount: 1);
        private readonly byte[] _filePathBytes;

        /// <summary>
        /// Instantiates a new <see cref="LinuxI2CBus"/> for a specified file path.
        /// </summary>
        /// <param name="filePath">The file path of the I²C bus.</param>
        public LinuxI2CBus(string filePath)
        {
            int len = Encoding.UTF8.GetByteCount(filePath) + 1;
            byte[] utf8FilePath = new byte[len];

            len = Encoding.UTF8.GetBytes(filePath, utf8FilePath);
            utf8FilePath[len] = 0;

            _filePathBytes = utf8FilePath;
        }

        /// <inheritdoc/>
        public override string ToString() =>
            Encoding.UTF8.GetString(_filePathBytes.AsSpan(0, _filePathBytes.Length - 1));

        /// <inheritdoc/>
        public override I2CDevice OpenDevice(int address) =>
            new LinuxI2CDevice(_filePathBytes, address, _sem);
    }
}
