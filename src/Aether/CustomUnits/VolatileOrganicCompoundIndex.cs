using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using UnitsNet;

namespace Aether.CustomUnits
{
    public enum VolatileOrganicCompoundIndexUnit
    {
        IndexValue
    }

    public readonly struct VolatileOrganicCompoundIndex : IQuantity<VolatileOrganicCompoundIndexUnit>, IEquatable<VolatileOrganicCompoundIndex>
    {
        public static VolatileOrganicCompoundIndex Zero => new VolatileOrganicCompoundIndex(0);

        public static QuantityInfo<VolatileOrganicCompoundIndexUnit> Info { get; } = new QuantityInfo<VolatileOrganicCompoundIndexUnit>(
            nameof(VolatileOrganicCompoundIndex),
            new UnitInfo<VolatileOrganicCompoundIndexUnit>[]
            {
                new (VolatileOrganicCompoundIndexUnit.IndexValue, "VocIndex", BaseUnits.Undefined)
            },
            VolatileOrganicCompoundIndexUnit.IndexValue,
            Zero,
            BaseDimensions.Dimensionless);

        public VolatileOrganicCompoundIndexUnit Unit { get; }

        Enum IQuantity.Unit =>
            Unit;

        public double Value { get; }

        [Obsolete]
        public QuantityType Type =>
            QuantityType.Information;

        public BaseDimensions Dimensions =>
            BaseDimensions.Dimensionless;

        public QuantityInfo<VolatileOrganicCompoundIndexUnit> QuantityInfo =>
            Info;

        QuantityInfo IQuantity.QuantityInfo =>
            QuantityInfo;

        static VolatileOrganicCompoundIndex()
        {
            UnitAbbreviationsCache.Default.MapUnitToAbbreviation(VolatileOrganicCompoundIndexUnit.IndexValue, "VOC Idx");
        }

        public VolatileOrganicCompoundIndex(double value, VolatileOrganicCompoundIndexUnit unit = VolatileOrganicCompoundIndexUnit.IndexValue)
        {
            Unit = unit;
            Value = value;
        }

        public double As(VolatileOrganicCompoundIndexUnit unit) =>
            unit == VolatileOrganicCompoundIndexUnit.IndexValue
            ? Value
            : throw new NotImplementedException($"Can not convert {Unit} to {unit}.");

        double IQuantity.As(Enum unit) =>
            unit is VolatileOrganicCompoundIndexUnit voc
            ? As(voc)
            : throw new ArgumentException($"The given unit is of type {unit.GetType()}. Only {typeof(VolatileOrganicCompoundIndexUnit)} is supported.", nameof(unit));

        public double As(UnitSystem unitSystem) =>
            Info.GetUnitInfosFor(unitSystem.BaseUnits).FirstOrDefault() is UnitInfo<VolatileOrganicCompoundIndexUnit> info
            ? As(info.Value)
            : throw new ArgumentException($"No units were found for the given {nameof(UnitSystem)}.", nameof(unitSystem));

        public VolatileOrganicCompoundIndex ToUnit(VolatileOrganicCompoundIndexUnit unit) =>
            unit == VolatileOrganicCompoundIndexUnit.IndexValue
            ? this
            : throw new NotImplementedException($"Can not convert {Unit} to {unit}.");

        IQuantity<VolatileOrganicCompoundIndexUnit> IQuantity<VolatileOrganicCompoundIndexUnit>.ToUnit(VolatileOrganicCompoundIndexUnit unit) =>
            ToUnit(unit);

        IQuantity IQuantity.ToUnit(Enum unit) =>
            unit is VolatileOrganicCompoundIndexUnit vocIndexUnit
            ? ToUnit(vocIndexUnit)
            : throw new ArgumentException($"Must be of type {nameof(VolatileOrganicCompoundIndexUnit)}.", nameof(unit));

        public VolatileOrganicCompoundIndex ToUnit(UnitSystem unitSystem) =>
            Info.GetUnitInfosFor(unitSystem.BaseUnits).FirstOrDefault() is UnitInfo<VolatileOrganicCompoundIndexUnit> info
            ? ToUnit(info.Value)
            : throw new ArgumentException($"No units were found for the given {nameof(UnitSystem)}.", nameof(unitSystem));

        IQuantity<VolatileOrganicCompoundIndexUnit> IQuantity<VolatileOrganicCompoundIndexUnit>.ToUnit(UnitSystem unitSystem) =>
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

        public bool Equals(VolatileOrganicCompoundIndex other) =>
            other.As(Unit).Equals(Value);

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is VolatileOrganicCompoundIndex other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Info.Name, Value, Unit);
    }
}
