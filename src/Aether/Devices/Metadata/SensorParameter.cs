namespace Aether.Devices.Metadata
{
    internal sealed class SensorParameter
    {
        public string Name { get; }
        public Type Type { get; }
        public string Description { get; }

        public SensorParameter(string name, Type type, string description)
        {
            Name = name;
            Type = type;
            Description = description;
        }
    }
}
