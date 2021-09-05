﻿using Aether.Devices.Sensors.Observable;
using System.Device.I2c;

namespace Aether.Devices.Sensors.Metadata
{
    internal abstract class I2cSensorInfo : SensorInfo
    {
        public abstract int DefaultAddress { get; }
        public abstract ObservableSensor OpenSensor(I2cDevice device, IEnumerable<ObservableSensor> dependencies);
        
        public ObservableSensor OpenDevice(int busId, int address, IEnumerable<ObservableSensor> dependencies)
        {
            I2cDevice device = I2cDevice.Create(new I2cConnectionSettings(busId, address));
            try
            {
                return OpenSensor(device, dependencies);
            }
            catch
            {
                device.Dispose();
                throw;
            }
        }
    }
}
