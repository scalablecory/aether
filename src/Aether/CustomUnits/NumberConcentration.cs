using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using UnitsNet;

namespace Aether.CustomUnits
{
    public enum NumberConcentrationUnit
    {
        ParticulatePerCubicCentimeter
    }

    public readonly struct NumberConcentration : IQuantity<NumberConcentrationUnit>, IEquatable<NumberConcentration>
    {
        public static NumberConcentration Zero => new NumberConcentration(0);

        public static QuantityInfo<NumberConcentrationUnit> Info { get; } = new QuantityInfo<NumberConcentrationUnit>(
            nameof(NumberConcentration),
            new UnitInfo<NumberConcentrationUnit>[]
            {
                new (NumberConcentrationUnit.ParticulatePerCubicCentimeter, "NumberConcentration", BaseUnits.Undefined)
            },
            NumberConcentrationUnit.ParticulatePerCubicCentimeter,
            Zero,
            BaseDimensions.Dimensionless);

        public NumberConcentrationUnit Unit { get; }

        Enum IQuantity.Unit =>
            Unit;

        public double Value { get; }

        public QuantityType Type =>
            QuantityType.Information;

        public BaseDimensions Dimensions =>
            BaseDimensions.Dimensionless;

        public QuantityInfo<NumberConcentrationUnit> QuantityInfo =>
            Info;

        QuantityInfo IQuantity.QuantityInfo =>
            QuantityInfo;

        static NumberConcentration()
        {
            UnitAbbreviationsCache.Default.MapUnitToAbbreviation(NumberConcentrationUnit.ParticulatePerCubicCentimeter, "per cm³");
        }

        public NumberConcentration(double value, NumberConcentrationUnit unit = NumberConcentrationUnit.ParticulatePerCubicCentimeter)
        {
            Unit = unit;
            Value = value;
        }

        public double As(NumberConcentrationUnit unit) =>
            unit == NumberConcentrationUnit.ParticulatePerCubicCentimeter
            ? Value
            : throw new NotImplementedException($"Can not convert {Unit} to {unit}.");

        double IQuantity.As(Enum unit) =>
            unit is NumberConcentrationUnit nc
            ? As(nc)
            : throw new ArgumentException($"The given unit is of type {unit.GetType()}. Only {typeof(NumberConcentrationUnit)} is supported.", nameof(unit));

        public double As(UnitSystem unitSystem) =>
            Info.GetUnitInfosFor(unitSystem.BaseUnits).FirstOrDefault() is UnitInfo<NumberConcentrationUnit> info
            ? As(info.Value)
            : throw new ArgumentException($"No units were found for the given {nameof(UnitSystem)}.", nameof(unitSystem));

        public NumberConcentration ToUnit(NumberConcentrationUnit unit) =>
            unit == NumberConcentrationUnit.ParticulatePerCubicCentimeter
            ? this
            : throw new NotImplementedException($"Can not convert {Unit} to {unit}.");

        IQuantity<NumberConcentrationUnit> IQuantity<NumberConcentrationUnit>.ToUnit(NumberConcentrationUnit unit) =>
            ToUnit(unit);

        IQuantity IQuantity.ToUnit(Enum unit) =>
            unit is NumberConcentrationUnit nc
            ? ToUnit(nc)
            : throw new ArgumentException($"Must be of type {nameof(NumberConcentrationUnit)}.", nameof(unit));

        public NumberConcentration ToUnit(UnitSystem unitSystem) =>
            Info.GetUnitInfosFor(unitSystem.BaseUnits).FirstOrDefault() is UnitInfo<NumberConcentrationUnit> info
            ? ToUnit(info.Value)
            : throw new ArgumentException($"No units were found for the given {nameof(UnitSystem)}.", nameof(unitSystem));

        IQuantity<NumberConcentrationUnit> IQuantity<NumberConcentrationUnit>.ToUnit(UnitSystem unitSystem) =>
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

        public bool Equals(NumberConcentration other) =>
            other.As(Unit).Equals(Value);

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is NumberConcentration other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Info.Name, Value, Unit);
    }
}
