namespace Aether.Devices.I2C
{
    /// <summary>
    /// An I²C bus.
    /// </summary>
    public abstract class I2CBus
    {
        /// <summary>
        /// Opens an I²C device at a specific address.
        /// </summary>
        /// <param name="address">The address of the device to open.</param>
        /// <returns>An <see cref="I2CDevice"/> for the address provided.</returns>
        public abstract I2CDevice OpenDevice(int address);
    }
}
