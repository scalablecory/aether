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
        /// Corresponds to a <see cref="Pressure"/>.
        /// </summary>
        BarometricPressure,

        /// <summary>
        /// Corresponds to a <see cref="MassConcentration"/> of particulate 0.3 - 1 micrometers.
        /// </summary>
        Particulate1_0PMassConcentration,

        /// <summary>
        /// Corresponds to a <see cref="MassConcentration"/> of particulate 0.3 - 2.5 micrometers.
        /// </summary>
        Particulate2_5PMassConcentration,

        /// <summary>
        /// Corresponds to a <see cref="MassConcentration"/> of particulate 0.3 - 4 micrometers.
        /// </summary>
        Particulate4_0PMassConcentration,

        /// <summary>
        /// Corresponds to a <see cref="MassConcentration"/> of particulate 0.3 - 10 micrometers.
        /// </summary>
        Particulate10_0PMassConcentration,

        /// <summary>
        /// Corresponds to a count of particulate 0.3 - 0.5 micrometers.
        /// </summary>
        Particulate0_5NumberConcentration,

        /// <summary>
        /// Corresponds to a count of particulate 0.3 - 1.0 micrometers.
        /// </summary>
        Particulate1_0NumberConcentration,

        /// <summary>
        /// Corresponds to a count of particulate 0.3 - 2.5 micrometers.
        /// </summary>
        Particulate2_5NumberConcentration,

        /// <summary>
        /// Corresponds to a count of particulate 0.3 - 4.0 micrometers.
        /// </summary>
        Particulate4_0NumberConcentration,

        /// <summary>
        /// Corresponds to a count of particulate 0.3 - 10.0 micrometers.
        /// </summary>
        Particulate10_0NumberConcentration
    }
}
