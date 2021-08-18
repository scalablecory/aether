namespace Aether.Devices
{
    internal sealed class SensorDependency
    {
        public Measure Measure { get; }
        public bool Required { get; }

        public SensorDependency(Measure measure, bool required)
        {
            Measure = measure;
            Required = required;
        }
    }
}
