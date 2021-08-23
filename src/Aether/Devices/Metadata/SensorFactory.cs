namespace Aether.Devices.Metadata
{
    /// <summary>
    /// Base class for a sensor factory.
    /// Used to create a sensor, populate config UI, and to wire dependencies and commands.
    /// </summary>
    internal abstract class SensorFactory
    {
        /// <summary>
        /// A collection of all factories supported by Aether.
        /// </summary>
        public static IReadOnlyList<SensorFactory> Factories { get; } = new List<SensorFactory>
        {
            new Sensors.SCD4xSensorFactory(),
            new Sensors.SHT4xSensorFactory()
        };

        public abstract string Manufacturer { get; }
        public abstract string Name { get; }
        public abstract string Uri { get; }
        public abstract IEnumerable<MeasureInfo> Measures { get; }
        public virtual IEnumerable<SensorDependency> Dependencies => Enumerable.Empty<SensorDependency>();
        public virtual IEnumerable<SensorCommand> Commands => Enumerable.Empty<SensorCommand>();
    }
}
