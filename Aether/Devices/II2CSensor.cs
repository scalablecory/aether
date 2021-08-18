using Aether.Devices.I2C;

namespace Aether.Devices
{
    internal interface II2CSensor : ISensor
    {
        static abstract int DefaultI2CAddress { get; }

        static abstract Sensor CreateFromI2C(I2CDevice device, IObservable<Measurement> dependencies);
    }
}
