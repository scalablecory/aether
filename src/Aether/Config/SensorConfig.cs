using Aether.Devices.Sensors.Metadata;
using System.Device.I2c;

namespace Aether.Config
{
    internal sealed class SensorConfig
    {
        public SensorInfo Sensor { get; }

        public I2cConnectionSettings? I2cSettings { get; }

        public IEnumerable<SensorInfo> Dependencies { get; }

        public SensorConfig(I2CSensorInfo sensor, I2cConnectionSettings i2cSettings, IEnumerable<SensorInfo> dependencies)
        {
            Sensor = sensor;
            I2cSettings = i2cSettings;
            Dependencies = dependencies;
        }
    }
}
