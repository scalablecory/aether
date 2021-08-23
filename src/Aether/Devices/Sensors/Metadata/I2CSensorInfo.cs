using Aether.Devices.Sensors.Observable;

namespace Aether.Devices.Sensors.Metadata
{
    abstract class I2CSensorInfo : SensorInfo
    {
        public abstract int DefaultAddress { get; }
        public abstract ObservableSensor OpenDevice(I2C.I2CDevice device, IObservable<Measurement> dependencies);
    }
}
