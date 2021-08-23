namespace Aether.Devices.Metadata
{
    internal sealed class SensorCommand
    {
        public string Name { get; }

        public IEnumerable<SensorParameter> Parameters { get; }

        public SensorCommand(string name, IEnumerable<SensorParameter> parameters)
        {
            Name = name;
            Parameters = parameters;
        }
    }
}
