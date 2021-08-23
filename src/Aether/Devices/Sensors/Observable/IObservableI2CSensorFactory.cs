namespace Aether.Devices.Sensors.Observable
{
    interface IObservableI2CSensorFactory : IObservableSensorFactory
    {
        static abstract int DefaultAddress { get; }
        static abstract ObservableSensor OpenDevice(I2C.I2CDevice device, IObservable<Measurement> dependencies);
    }
}
