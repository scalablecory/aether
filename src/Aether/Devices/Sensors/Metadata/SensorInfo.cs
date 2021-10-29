using System.Device.I2c;

namespace Aether.Devices.Sensors.Metadata
{
    /// <summary>
    /// Base class for a sensor factory.
    /// Used to create a sensor, populate config UI, and to wire dependencies and commands.
    /// </summary>
    internal abstract class SensorInfo
    {
        /// <summary>
        /// A collection of all sensors supported by Aether.
        /// </summary>
        public static IReadOnlyList<SensorInfo> Sensors { get; } = new List<SensorInfo>
        {
            new ConcreteI2CSensorInfo<ObservableScd4x>(),
            new ConcreteI2CSensorInfo<ObservableSht4x>(),
            new SimulatedI2cSensorInfo<ObservableMs5637>(),
            new ConcreteI2CSensorInfo<ObservableSgp4x>(),
            new ConcreteI2CSensorInfo<ObservableSps30>()
        };

        public abstract string Manufacturer { get; }
        public abstract string Name { get; }
        public abstract string Uri { get; }
        public abstract IEnumerable<MeasureInfo> Measures { get; }
        public abstract IEnumerable<SensorDependency> Dependencies { get; }
        public abstract IEnumerable<SensorCommand> Commands { get; }

        public abstract bool CanSimulateSensor { get; }
        public abstract ObservableSensor CreateSimulatedSensor(IObservable<Measurement> dependencies);

        private class ConcreteI2CSensorInfo<T> : I2cSensorInfo
            where T : IObservableI2cSensorFactory
        {
            public override int DefaultAddress => T.DefaultAddress;

            public override string Manufacturer => T.Manufacturer;

            public override string Name => T.Name;

            public override string Uri => T.Uri;

            public override IEnumerable<MeasureInfo> Measures => T.Measures;
            public override IEnumerable<SensorDependency> Dependencies => T.Dependencies;
            public override IEnumerable<SensorCommand> Commands => T.Commands;

            public override bool CanSimulateSensor => false;

            public override ObservableSensor OpenSensor(I2cDevice device, IObservable<Measurement> dependencies) =>
                T.OpenSensor(device, dependencies);

            public override ObservableSensor CreateSimulatedSensor(IObservable<Measurement> dependencies) =>
                throw new NotImplementedException();
        }

        private sealed class SimulatedI2cSensorInfo<T> : ConcreteI2CSensorInfo<T>
            where T : IObservableI2cSensorFactory, ISimulatedI2cDeviceFactory
        {
            public override bool CanSimulateSensor => true;

            public override ObservableSensor CreateSimulatedSensor(IObservable<Measurement> dependencies)
            {
                I2cDevice device = T.CreateSimulatedI2cDevice();
                try
                {
                    return OpenSensor(device, dependencies);
                }
                catch
                {
                    device.Dispose();
                    throw;
                }
            }
        }
    }
}
