using System.Device.I2c;

namespace Aether.Devices.Sensors.Observable
{
    internal interface ISimulatedI2cDeviceFactory
    {
        static abstract I2cDevice CreateSimulatedI2cDevice();
    }
}
