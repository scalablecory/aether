using Aether.Devices.Sensors;
using System.Device.I2c;

namespace Aether.Config
{
    internal sealed class SensorConfig
    {
        public SensorFactory Sensor { get; }

        public I2cConnectionSettings? I2cSettings { get; }

        public IEnumerable<SensorFactory> Dependencies { get; }

        public SensorConfig(I2cSensorFactory sensor, I2cConnectionSettings i2cSettings, IEnumerable<SensorFactory> dependencies)
        {
            Sensor = sensor;
            I2cSettings = i2cSettings;
            Dependencies = dependencies;
        }
    }
}
