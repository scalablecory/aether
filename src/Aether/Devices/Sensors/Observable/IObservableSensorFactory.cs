using Aether.Devices.Sensors.Metadata;

namespace Aether.Devices.Sensors.Observable
{
    internal interface IObservableSensorFactory
    {
        static abstract string Manufacturer { get; }
        static abstract string Name { get; }
        static abstract string Uri { get; }
        static abstract IEnumerable<MeasureInfo> Measures { get; }
        static abstract IEnumerable<SensorDependency> Dependencies { get; }
        static abstract IEnumerable<SensorCommand> Commands { get; }
    }
}
