using System.Runtime.InteropServices;
using Aether.CustomUnits;
using UnitsNet;
using UnitsNet.Units;

namespace Aether.Devices.Sensors
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct Measurement
    {
        private readonly double _value;
        private readonly int _unit;
        private readonly Measure _measure;

        public Measure Measure => _measure;

        public RelativeHumidity RelativeHumidity =>
            new RelativeHumidity(_value, (RelativeHumidityUnit)_unit);

        public Temperature Temperature =>
            new Temperature(_value, (TemperatureUnit)_unit);

        public VolumeConcentration Co2 =>
            new VolumeConcentration(_value, (VolumeConcentrationUnit)_unit);

        public Pressure BarometricPressure =>
            new Pressure(_value, (PressureUnit)_unit);

        public  VolatileOrganicCompoundIndex Voc =>
            new VolatileOrganicCompoundIndex(_value, (VolatileOrganicCompoundIndexUnit)_unit);

        public MassConcentration MassConcentration =>
            new MassConcentration(_value, (MassConcentrationUnit)_unit);

        public NumberConcentration NumberConcentration =>
            new NumberConcentration(_value, (NumberConcentrationUnit)_unit);

        public Length Length =>
            new Length(_value, (LengthUnit)_unit);

        public AirQualityIndex AirQualityIndex =>
            new AirQualityIndex(_value, (AirQualityIndexUnit)_unit);

        private Measurement(double value, int unit, Measure measure)
        {
            _value = value;
            _unit = unit;
            _measure = measure;
        }

        public static Measurement FromRelativeHumidity(RelativeHumidity h) =>
            new Measurement(h.Value, (int)h.Unit, Measure.Humidity);

        public static Measurement FromTemperature(Temperature t) =>
            new Measurement(t.Value, (int)t.Unit, Measure.Temperature);

        public static Measurement FromCo2(VolumeConcentration co2) =>
            new Measurement(co2.Value, (int)co2.Unit, Measure.CO2);

        public static Measurement FromPressure(Pressure p) =>
            new Measurement(p.Value, (int)p.Unit, Measure.BarometricPressure);

        public static Measurement FromVoc(VolatileOrganicCompoundIndex vocIndex) =>
            new Measurement(vocIndex.Value, (int)vocIndex.Unit, Measure.VOC);

        public static Measurement FromPM1_0(MassConcentration massConcentration) =>
            new Measurement(massConcentration.Value, (int)massConcentration.Unit, Measure.PM1_0);

        public static Measurement FromPM2_5(MassConcentration massConcentration) =>
            new Measurement(massConcentration.Value, (int)massConcentration.Unit, Measure.PM2_5);

        public static Measurement FromPM4_0(MassConcentration massConcentration) =>
            new Measurement(massConcentration.Value, (int)massConcentration.Unit, Measure.PM4_0);

        public static Measurement FromPM10_0(MassConcentration massConcentration) =>
            new Measurement(massConcentration.Value, (int)massConcentration.Unit, Measure.PM10_0);

        public static Measurement FromP0_5(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.P0_5);

        public static Measurement FromP1_0(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.P1_0);

        public static Measurement FromP2_5(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.P2_5);

        public static Measurement FromP4_0(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.P4_0);

        public static Measurement FromP10_0(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.P10_0);

        public static Measurement FromTypicalParticleSize(Length length) =>
            new Measurement(length.Value, (int)length.Unit, Measure.TypicalParticleSize);

        public static Measurement FromAirQualityIndex(AirQualityIndex airQualityIndex) =>
            new Measurement(airQualityIndex.Value, (int)airQualityIndex.Unit, Measure.AirQualityIndex);

        public override string ToString() => Measure switch
        {
            Measure.Humidity => RelativeHumidity.ToString(),
            Measure.Temperature => Temperature.ToString(),
            Measure.CO2 => Co2.ToString(),
            Measure.BarometricPressure => BarometricPressure.ToString(),
            Measure.VOC => Voc.ToString(),
            Measure.PM1_0 => MassConcentration.ToString(),
            Measure.PM2_5 => MassConcentration.ToString(),
            Measure.PM4_0 => MassConcentration.ToString(),
            Measure.PM10_0 => MassConcentration.ToString(),
            Measure.P0_5 => NumberConcentration.ToString(),
            Measure.P1_0 => NumberConcentration.ToString(),
            Measure.P2_5 => NumberConcentration.ToString(),
            Measure.P4_0 => NumberConcentration.ToString(),
            Measure.P10_0 => NumberConcentration.ToString(),
            Measure.TypicalParticleSize => Length.ToString(),
            Measure.AirQualityIndex => AirQualityIndex.ToString(),
            _ => $"{{ Empty {nameof(Measurement)} }}"
        };
    }
}
