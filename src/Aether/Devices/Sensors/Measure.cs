using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal enum Measure
    {
        None,

        /// <summary>
        /// Corresponds to a <see cref="RelativeHumidity"/>.
        /// </summary>
        Humidity,

        /// <summary>
        /// Corresponds to a <see cref="UnitsNet.Temperature"/>.
        /// </summary>
        Temperature,

        /// <summary>
        /// Corresponds to a <see cref="VolumeConcentration"/>.
        /// </summary>
        CO2,

        /// <summary>
        /// Corresponds to a <see cref="VolumeConcentration"/>.
        /// </summary>
        VOC,

        /// <summary>
        /// Corresponds to a <see cref="UnitsNet.Pressure"/>.
        /// </summary>
        BarometricPressure
    }
}
