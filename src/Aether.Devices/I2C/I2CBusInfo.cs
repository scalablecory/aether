namespace Aether.Devices.I2C
{
    /// <summary>
    /// Information about an I²C bus.
    /// </summary>
    public abstract class I2CBusInfo
    {
        /// <summary>
        /// A user-readable name for the I²C bus.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Opens the I²C bus.
        /// </summary>
        /// <returns>A new <see cref="I2CBus"/>.</returns>
        public abstract I2CBus Open();
    }
}
