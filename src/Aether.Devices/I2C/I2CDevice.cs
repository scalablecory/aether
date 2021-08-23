namespace Aether.Devices.I2C
{
    /// <summary>
    /// An I²C device corresponding to a single address on an <see cref="I2CBus"/>.
    /// </summary>
    public abstract class I2CDevice : IDisposable
    {
        /// <inheritdoc/>
        public abstract void Dispose();

        /// <summary>
        /// Performs an I²C write sequence:
        /// <code>
        /// S Addr Wr [A] WriteBuffer0 [A] ... WriteBufferN [A]
        /// P
        /// </code>
        /// </summary>
        public abstract ValueTask WriteAsync(ReadOnlyMemory<byte> writeBuffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs an I²C read sequence:
        /// <code>
        /// S Addr Rd [A] ReadBuffer0 [A] ... ReadBufferN [NA]
        /// P
        /// </code>
        /// </summary>
        public abstract ValueTask ReadAsync(Memory<byte> readBuffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a combined I²C write+read sequence:
        /// <code>
        /// S Addr Wr [A] WriteBuffer0 [A] ... WriteBufferN [A]
        /// S Addr Rd [A] ReadBuffer0 [A] ... ReadBufferN [NA]
        /// P
        /// </code>
        /// </summary>
        /// <remarks>
        /// An <see cref="I2CBus"/> that doesn't natively support this may emulate this with an additional <code>P</code> (stop) byte:
        /// <code>
        /// S Addr Wr [A] WriteBuffer0 [A] ... WriteBufferN [A]
        /// P
        /// S Addr Rd [A] ReadBuffer0 [A] ... ReadBufferN [NA]
        /// P
        /// </code>
        /// </remarks>
        public abstract ValueTask WriteAndReadAsync(ReadOnlyMemory<byte> writeBuffer, Memory<byte> readBuffer, CancellationToken cancellationToken = default);
    }
}
