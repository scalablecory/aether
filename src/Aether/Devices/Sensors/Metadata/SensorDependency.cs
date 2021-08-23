namespace Aether.Devices.Sensors.Metadata
{
    internal sealed class SensorDependency
    {
        public static IEnumerable<SensorDependency> NoDependencies { get; } = new List<SensorDependency>();

        public Measure Measure { get; }
        public bool Required { get; }

        public SensorDependency(Measure measure, bool required)
        {
            Measure = measure;
            Required = required;
        }
    }
}
