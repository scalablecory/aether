namespace Aether.Devices.I2C
{
    /// <summary>
    /// A base class for an I2C bus.
    /// </summary>
    internal abstract class I2CBus : IDisposable
    {
        public abstract void Dispose();

        public abstract I2CDevice OpenDevice(int address);
    }
}
