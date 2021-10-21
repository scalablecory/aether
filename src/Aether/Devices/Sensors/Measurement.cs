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

        public NumberConcentration NumberConcentration =>
            new NumberConcentration(_value, (NumberConcentrationUnit)_unit);

        public Length Length =>
            new Length(_value, (LengthUnit)_unit);

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

        public static Measurement From1_0PMassConcentration(MassConcentration massConcentration) =>
            new Measurement(massConcentration.MicrogramsPerCubicMeter, (int)massConcentration.Unit, Measure.Particulate1_0PMassConcentration);

        public static Measurement From2_5PMassConcentration(MassConcentration massConcentration) =>
            new Measurement(massConcentration.MicrogramsPerCubicMeter, (int)massConcentration.Unit, Measure.Particulate2_5PMassConcentration);

        public static Measurement From4_0PMassConcentration(MassConcentration massConcentration) =>
            new Measurement(massConcentration.MicrogramsPerCubicMeter, (int)massConcentration.Unit, Measure.Particulate4_0PMassConcentration);

        public static Measurement From10_0PMassConcentration(MassConcentration massConcentration) =>
             new Measurement(massConcentration.MicrogramsPerCubicMeter, (int)massConcentration.Unit, Measure.Particulate10_0PMassConcentration);

        public static Measurement From0_5NumberConcentration(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.Particulate0_5NumberConcentration);

        public static Measurement From1_0NumberConcentration(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.Particulate1_0NumberConcentration);

        public static Measurement From2_5NumberConcentration(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.Particulate2_5NumberConcentration);

        public static Measurement From4_0NumberConcentration(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.Particulate4_0NumberConcentration);

        public static Measurement From10_0NumberConcentration(NumberConcentration numberConcentration) =>
            new Measurement(numberConcentration.Value, 0, Measure.Particulate10_0NumberConcentration);

        public static Measurement FromParticulateTypicalSize(Length length) =>
            new Measurement(length.Value, (int)length.Unit, Measure.ParticulateTypicalSize);

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
            Measure.Particulate0_5NumberConcentration => NumberConcentration.ToString(),
            Measure.Particulate1_0NumberConcentration => NumberConcentration.ToString(),
            Measure.Particulate2_5NumberConcentration => NumberConcentration.ToString(),
            Measure.Particulate4_0NumberConcentration => NumberConcentration.ToString(),
            Measure.Particulate10_0NumberConcentration => NumberConcentration.ToString(),
            Measure.ParticulateTypicalSize => Length.ToString(),
            _ => $"{{ Empty {nameof(Measurement)} }}"
        };
    }
}
