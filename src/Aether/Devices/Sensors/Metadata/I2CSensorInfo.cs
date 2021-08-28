using Aether.Devices.Sensors.Observable;
using System.Device.I2c;

namespace Aether.Devices.Sensors.Metadata
{
    internal abstract class I2CSensorInfo : SensorInfo
    {
        public abstract int DefaultAddress { get; }
        public abstract ObservableSensor OpenDevice(I2cDevice device, IObservable<Measurement> dependencies);
    }
}
