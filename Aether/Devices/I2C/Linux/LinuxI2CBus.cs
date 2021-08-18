namespace Aether.Devices.I2C.Linux
{
    /// <summary>
    /// An I2C bus for Linux, via SMBus APIs.
    /// </summary>
    internal sealed class LinuxI2CBus
    {
        private readonly SemaphoreSlim _sem = new(initialCount: 1);

        public Task LockAsync(CancellationToken cancellationToken) =>
            _sem.WaitAsync(cancellationToken);

        public void Unlock() =>
            _sem.Release();
    }
}
