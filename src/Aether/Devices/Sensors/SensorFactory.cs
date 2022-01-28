using Aether.Devices.Sensors.Metadata;

namespace Aether.Devices.Sensors
{
    internal abstract class SensorFactory
    {
        /// <summary>
        /// A collection of all sensors supported by Aether.
        /// </summary>
        public static IReadOnlyList<SensorFactory> Sensors { get; } = new List<SensorFactory>
        {
            ObservableScd4x.Instance,
            ObservableSht4x.Instance,
            ObservableMs5637.Instance,
            ObservableSgp4x.Instance,
            ObservableSps30.Instance
        };

        public abstract string Manufacturer { get; }
        public abstract string Name { get; }
        public abstract string Uri { get; }
        public abstract IEnumerable<MeasureInfo> Measures { get; }
        public abstract IEnumerable<SensorDependency> Dependencies { get; }
        public abstract IEnumerable<SensorCommand> Commands { get; }
        public virtual bool CanSimulate => false;
        public virtual IObservable<Measurement> OpenSimulatedSensor(IObservable<Measurement> dependencies) =>
            throw new NotImplementedException();
    }
}
