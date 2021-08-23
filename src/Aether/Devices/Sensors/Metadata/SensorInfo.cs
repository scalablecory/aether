using Aether.Devices.I2C;
using Aether.Devices.Sensors.Observable;

namespace Aether.Devices.Sensors.Metadata
{
    /// <summary>
    /// Base class for a sensor factory.
    /// Used to create a sensor, populate config UI, and to wire dependencies and commands.
    /// </summary>
    internal abstract class SensorInfo
    {
        /// <summary>
        /// A collection of all factories supported by Aether.
        /// </summary>
        public static IReadOnlyList<SensorInfo> Sensors { get; } = new List<SensorInfo>
        {
            new ConcreteI2CSensorInfo<ObservableSCD4x>(),
            new ConcreteI2CSensorInfo<ObservableSHT4x>()
        };

        public abstract string Manufacturer { get; }
        public abstract string Name { get; }
        public abstract string Uri { get; }
        public abstract IEnumerable<MeasureInfo> Measures { get; }
        public virtual IEnumerable<SensorDependency> Dependencies => Enumerable.Empty<SensorDependency>();
        public virtual IEnumerable<SensorCommand> Commands => Enumerable.Empty<SensorCommand>();

        private class ConcreteI2CSensorInfo<T> : I2CSensorInfo
            where T : IObservableI2CSensorFactory
        {
            public override int DefaultAddress => T.DefaultAddress;

            public override string Manufacturer => T.Manufacturer;

            public override string Name => T.Name;

            public override string Uri => T.Uri;

            public override IEnumerable<MeasureInfo> Measures => T.Measures;

            public override ObservableSensor OpenDevice(I2CDevice device, IObservable<Measurement> dependencies) =>
                T.OpenDevice(device, dependencies);
        }
    }
}
