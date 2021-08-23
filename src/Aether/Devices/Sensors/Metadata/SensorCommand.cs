namespace Aether.Devices.Sensors.Metadata
{
    internal sealed class SensorCommand
    {
        public static IEnumerable<SensorCommand> NoCommands { get; } = new List<SensorCommand>();

        public string Name { get; }

        public IEnumerable<SensorParameter> Parameters { get; }

        public Type? ReturnType { get; }

        public SensorCommand(string name, IEnumerable<SensorParameter> parameters)
        {
            Name = name;
            Parameters = parameters;
        }
    }
}
