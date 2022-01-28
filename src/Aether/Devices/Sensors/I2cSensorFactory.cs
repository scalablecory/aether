using System.Device.I2c;

namespace Aether.Devices.Sensors
{
    internal abstract class I2cSensorFactory : SensorFactory
    {
        public abstract int DefaultAddress { get; }

        public abstract IObservable<Measurement> OpenSensor(Func<I2cDevice> deviceFunc, IObservable<Measurement> dependencies);

        public IObservable<Measurement> OpenSensor(int busId, int deviceId, IObservable<Measurement> dependencies)
        {
            var settings = new I2cConnectionSettings(busId, deviceId);
            I2cDevice CreateDevice() => I2cDevice.Create(settings);

            return OpenSensor(CreateDevice, dependencies);
        }

        public sealed override IObservable<Measurement> OpenSimulatedSensor(IObservable<Measurement> dependencies) =>
            OpenSensor(CreateSimulatedI2cDevice, dependencies);

        protected virtual I2cDevice CreateSimulatedI2cDevice() =>
            throw new NotImplementedException();
    }
}
