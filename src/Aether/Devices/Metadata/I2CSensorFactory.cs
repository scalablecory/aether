using Aether.Devices.I2C;

namespace Aether.Devices.Metadata
{
    /// <summary>
    /// A factory for I2C-based sensors. <inheritdoc cref="SensorFactory" path="summary/."/>
    /// </summary>
    internal abstract class I2CSensorFactory : SensorFactory
    {
        public abstract int DefaultAddress { get; }
        public abstract IObservable<Measurement> OpenDevice(I2CDevice device, IObservable<Measurement> dependencies);
    }
}
