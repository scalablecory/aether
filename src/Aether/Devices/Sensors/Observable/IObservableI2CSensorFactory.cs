using System.Device.I2c;

namespace Aether.Devices.Sensors.Observable
{
    interface IObservableI2CSensorFactory : IObservableSensorFactory
    {
        static abstract int DefaultAddress { get; }
        static abstract ObservableSensor OpenDevice(I2cDevice device, IObservable<Measurement> dependencies);
    }
}
