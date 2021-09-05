using System.Device.I2c;

namespace Aether.Devices.Sensors.Observable
{
    interface IObservableI2cSensorFactory : IObservableSensorFactory
    {
        static abstract int DefaultAddress { get; }
        static abstract ObservableSensor OpenSensor(I2cDevice device, IEnumerable<ObservableSensor> dependencies);
    }
}
