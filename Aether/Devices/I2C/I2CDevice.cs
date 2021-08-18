namespace Aether.Devices.I2C
{
    /// <summary>
    /// A base class for an I2C device.
    /// </summary>
    internal abstract class I2CDevice : IDisposable
    {
        public abstract void Dispose();

        /// <remarks>
        /// S Addr Wr [A] WriteBuffer0 [A] ... WriteBufferN [A]
        /// P
        /// </remarks>
        public abstract ValueTask WriteAsync(ReadOnlyMemory<byte> writeBuffer, CancellationToken cancellationToken);

        /// <remarks>
        /// S Addr Rd [A] ReadBuffer0 [A] ... ReadBufferN [NA]
        /// P
        /// </remarks>
        public abstract ValueTask ReadAsync(Memory<byte> readBuffer, CancellationToken cancellationToken);

        /// <remarks>
        /// S Addr Wr [A] WriteBuffer0 [A] ... WriteBufferN [A]
        /// S Addr Rd [A] ReadBuffer0 [A] ... ReadBufferN [NA]
        /// P
        /// </remarks>
        public abstract ValueTask WriteAndReadAsync(ReadOnlyMemory<byte> writeBuffer, Memory<byte> readBuffer, CancellationToken cancellationToken);
    }
}
