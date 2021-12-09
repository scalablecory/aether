using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using UnitsNet;

namespace Aether.CustomUnits
{
    public enum AirQualityIndexUnit
    {
        EPA
    }

    public readonly struct AirQualityIndex : IQuantity<AirQualityIndexUnit>, IEquatable<AirQualityIndex>
    {
        public static AirQualityIndex Zero => new AirQualityIndex(0);

        public static QuantityInfo<AirQualityIndexUnit> Info { get; } = new QuantityInfo<AirQualityIndexUnit>(
            nameof(AirQualityIndex),
            new UnitInfo<AirQualityIndexUnit>[]
            {
                new (AirQualityIndexUnit.EPA, BaseUnits.Undefined)
            },
            AirQualityIndexUnit.EPA,
            Zero,
            BaseDimensions.Dimensionless);

        public AirQualityIndexUnit Unit { get; }

        Enum IQuantity.Unit =>
            Unit;

        public double Value { get; }

        public QuantityType Type =>
            QuantityType.Information;

        public BaseDimensions Dimensions =>
            BaseDimensions.Dimensionless;

        public QuantityInfo<AirQualityIndexUnit> QuantityInfo =>
            Info;

        QuantityInfo IQuantity.QuantityInfo =>
            QuantityInfo;

        static AirQualityIndex()
        {
            UnitAbbreviationsCache.Default.MapUnitToAbbreviation(AirQualityIndexUnit.EPA, "AQI");
        }

        public AirQualityIndex(double value, AirQualityIndexUnit unit = AirQualityIndexUnit.EPA)
        {
            Unit = unit;
            Value = value;
        }

        public double As(AirQualityIndexUnit unit) =>
            unit == AirQualityIndexUnit.EPA
            ? Value
            : throw new NotImplementedException($"Can not convert {Unit} to {unit}.");

        double IQuantity.As(Enum unit) =>
            unit is AirQualityIndexUnit aqi
            ? As(aqi)
            : throw new ArgumentException($"The given unit is of type {unit.GetType()}. Only {typeof(AirQualityIndexUnit)} is supported.", nameof(unit));

        public double As(UnitSystem unitSystem) =>
            Info.GetUnitInfosFor(unitSystem.BaseUnits).FirstOrDefault() is UnitInfo<AirQualityIndexUnit> info
            ? As(info.Value)
            : throw new ArgumentException($"No units were found for the given {nameof(UnitSystem)}.", nameof(unitSystem));

        public AirQualityIndex ToUnit(AirQualityIndexUnit unit) =>
            unit == AirQualityIndexUnit.EPA
            ? this
            : throw new NotImplementedException($"Can not convert {Unit} to {unit}.");

        IQuantity<AirQualityIndexUnit> IQuantity<AirQualityIndexUnit>.ToUnit(AirQualityIndexUnit unit) =>
            ToUnit(unit);

        IQuantity IQuantity.ToUnit(Enum unit) =>
            unit is AirQualityIndexUnit aqi
            ? ToUnit(aqi)
            : throw new ArgumentException($"Must be of type {nameof(AirQualityIndexUnit)}.", nameof(unit));

        public AirQualityIndex ToUnit(UnitSystem unitSystem) =>
            Info.GetUnitInfosFor(unitSystem.BaseUnits).FirstOrDefault() is UnitInfo<AirQualityIndexUnit> info
            ? ToUnit(info.Value)
            : throw new ArgumentException($"No units were found for the given {nameof(UnitSystem)}.", nameof(unitSystem));

        IQuantity<AirQualityIndexUnit> IQuantity<AirQualityIndexUnit>.ToUnit(UnitSystem unitSystem) =>
            ToUnit(unitSystem);

        IQuantity IQuantity.ToUnit(UnitSystem unitSystem) =>
            ToUnit(unitSystem);

        public string ToString(string? format, IFormatProvider? formatProvider) =>
            QuantityFormatter.Format(this, format ?? "g", formatProvider);

        public override string ToString() =>
            ToString(null, null);

        public string ToString(IFormatProvider? provider) =>
            ToString(null, provider);

        string IQuantity.ToString(IFormatProvider? provider, int significantDigitsAfterRadix)
        {
            string format = string.Create(CultureInfo.InvariantCulture, $"s{significantDigitsAfterRadix}");
            return ToString(format, provider);
        }

        string IQuantity.ToString(IFormatProvider? provider, string format, params object[] args) =>
            throw new NotImplementedException();

        public bool Equals(AirQualityIndex other) =>
            other.As(Unit).Equals(Value);

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is AirQualityIndex other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Info.Name, Value, Unit);
    }
}
