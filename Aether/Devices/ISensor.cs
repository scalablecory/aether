namespace Aether.Devices
{
    internal interface ISensor
    {
        static abstract string Name { get; }
        static abstract IEnumerable<SensorDependency> Dependencies { get; }
        static abstract IEnumerable<MeasureInfo> AvailableMeasures { get; }
    }
}
