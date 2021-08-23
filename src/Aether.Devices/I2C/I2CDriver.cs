namespace Aether.Devices.I2C
{
    /// <summary>
    /// An I²C driver that manages zero or more connected <see cref="I2CBus"/>.
    /// </summary>
    public abstract class I2CDriver
    {
        /// <summary>
        /// Enumerates I²C busses managed by the driver.
        /// </summary>
        /// <returns>A collection of <see cref="I2CBusInfo"/> describing available I²C busses.</returns>
        public abstract IEnumerable<I2CBusInfo> EnumerateBusses();
    }
}
