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
            new Measurement(p.Value, (int)p.Unit, Measure.Humidity);

        public override string ToString() => Measure switch
        {
            Measure.Humidity => RelativeHumidity.ToString(),
            Measure.Temperature => Temperature.ToString(),
            Measure.CO2 => Co2.ToString(),
            Measure.BarometricPressure => BarometricPressure.ToString(),
            _ => $"{{ Empty {nameof(Measurement)} }}"
        };
    }
}
