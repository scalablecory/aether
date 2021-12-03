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
        /// Corresponds to a <see cref="VolatileOrganicCompoundIndex"/>.
        /// </summary>
        VOC,

        /// <summary>
        /// Corresponds to a <see cref="Pressure"/>.
        /// </summary>
        BarometricPressure,

        /// <summary>
        /// Corresponds to a <see cref="MassConcentration"/> of particulate 0.3 - 1 micrometers.
        /// </summary>
        PM1_0,

        /// <summary>
        /// Corresponds to a <see cref="MassConcentration"/> of particulate 0.3 - 2.5 micrometers.
        /// </summary>
        PM2_5,

        /// <summary>
        /// Corresponds to a <see cref="MassConcentration"/> of particulate 0.3 - 4 micrometers.
        /// </summary>
        PM4_0,

        /// <summary>
        /// Corresponds to a <see cref="MassConcentration"/> of particulate 0.3 - 10 micrometers.
        /// </summary>
        PM10_0,

        /// <summary>
        /// Corresponds to a <see cref="NumberConcentration"/> of a count of particulate 0.3 - 0.5 micrometers.
        /// </summary>
        P0_5,

        /// <summary>
        /// Corresponds to a <see cref="NumberConcentration"/> of a count of particulate 0.3 - 1 micrometers.
        /// </summary>
        P1_0,
        /// <summary>
        /// Corresponds to a <see cref="NumberConcentration"/> of a count of particulate 0.3 - 2.5 micrometers.
        /// </summary>
        P2_5,
        /// <summary>
        /// Corresponds to a <see cref="NumberConcentration"/> of a count of particulate 0.3 - 4 micrometers.
        /// </summary>
        P4_0,
        /// <summary>
        /// Corresponds to a <see cref="NumberConcentration"/> of a count of particulate 0.3 - 10 micrometers.
        /// </summary>
        P10_0,

        /// <summary>
        /// Corresponds to a <see cref="Length"/> of the typical size of particles.
        /// </summary>
        TypicalParticleSize,

        /// <summary>
        /// An air quality index, derived from other measures.
        /// </summary>
        AirQualityIndex
    }
}
