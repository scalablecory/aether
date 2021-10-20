using Aether.CustomUnits;
using Aether.Devices.Drivers;
using System.Runtime.InteropServices;
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

        public static Measurement From1_0PMassConcentraiton(MassConcentration massConcentration) =>
            new Measurement(massConcentration.MicrogramsPerCubicMeter, (int)massConcentration.Unit, Measure.Particulate1_0PMassConcentration);

        public static Measurement From2_5PMassConcentraiton(MassConcentration massConcentration) =>
            new Measurement(massConcentration.MicrogramsPerCubicMeter, (int)massConcentration.Unit, Measure.Particulate2_5PMassConcentration);

        public static Measurement From4_0PMassConcentraiton(MassConcentration massConcentration) =>
            new Measurement(massConcentration.MicrogramsPerCubicMeter, (int)massConcentration.Unit, Measure.Particulate4_0PMassConcentration);

        public static Measurement From10_0PMassConcentraiton(MassConcentration massConcentration) =>
             new Measurement(massConcentration.MicrogramsPerCubicMeter, (int)massConcentration.Unit, Measure.Particulate10_0PMassConcentration);

        public static Measurement From0_5NumberConcentraiton(float numberConcentration) =>
            new Measurement(numberConcentration, 0, Measure.Particulate0_5NumberConcentration);

        public static Measurement From1_0NumberConcentraiton(float numberConcentration) =>
            new Measurement(numberConcentration, 0, Measure.Particulate1_0NumberConcentration);

        public static Measurement From2_5NumberConcentraiton(float numberConcentration) =>
            new Measurement(numberConcentration, 0, Measure.Particulate2_5NumberConcentration);

        public static Measurement From4_0NumberConcentraiton(float numberConcentration) =>
            new Measurement(numberConcentration, 0, Measure.Particulate4_0NumberConcentration);

        public static Measurement From10_0NumberConcentraiton(float numberConcentration) =>
            new Measurement(numberConcentration, 0, Measure.Particulate10_0NumberConcentration);

        public override string ToString() => Measure switch
        {
            Measure.Humidity => RelativeHumidity.ToString(),
            Measure.Temperature => Temperature.ToString(),
            Measure.CO2 => Co2.ToString(),
            Measure.BarometricPressure => BarometricPressure.ToString(),
            Measure.VOC => Voc.ToString(),
            Measure.Particulate1_0PMassConcentration => MassConcentration.ToString(),
            Measure.Particulate2_5PMassConcentration => MassConcentration.ToString(),
            Measure.Particulate4_0PMassConcentration => MassConcentration.ToString(),
            Measure.Particulate10_0PMassConcentration => MassConcentration.ToString(),
            Measure.Particulate0_5NumberConcentration => $"{_value}/Cubic CentiMeter",
            Measure.Particulate1_0NumberConcentration => $"{_value}/Cubic CentiMeter",
            Measure.Particulate2_5NumberConcentration => $"{_value}/Cubic CentiMeter",
            Measure.Particulate4_0NumberConcentration => $"{_value}/Cubic CentiMeter",
            Measure.Particulate10_0NumberConcentration => $"{_value}/Cubic CentiMeter",
            _ => $"{{ Empty {nameof(Measurement)} }}"
        };
    }
}
