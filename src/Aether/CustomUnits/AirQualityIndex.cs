using System.Globalization;
using UnitsNet;

namespace Aether.CustomUnits
{
    public enum AirQualityIndexUnit
    {
        EPA
    }

    public struct AirQualityIndex : IQuantity
    {
        public AirQualityIndex(double value, AirQualityIndexUnit unit = AirQualityIndexUnit.EPA)
        {
            Unit = unit;
            Value = value;
        }

        Enum IQuantity.Unit => Unit;
        public AirQualityIndexUnit Unit { get; }

        public double Value { get; }

        #region IQuantity

        private static readonly AirQualityIndex Zero = new AirQualityIndex(0, AirQualityIndexUnit.EPA);

        public QuantityType Type => QuantityType.Information;
        public BaseDimensions Dimensions => BaseDimensions.Dimensionless;

        public QuantityInfo QuantityInfo => new QuantityInfo(
            nameof(AirQualityIndexUnit),
            typeof(AirQualityIndexUnit),
            new UnitInfo[]
            {
                new UnitInfo<AirQualityIndexUnit>(AirQualityIndexUnit.EPA, BaseUnits.Undefined)
            },
            AirQualityIndexUnit.EPA,
            Zero,
            BaseDimensions.Dimensionless);

        public double As(Enum unit) => unit is AirQualityIndexUnit airQualityIndexUnit
            ? Value
            : throw new ArgumentException($"Must be of type {nameof(AirQualityIndexUnit)}.", nameof(unit));

        public double As(UnitSystem unitSystem) =>
            throw new NotImplementedException();

        public IQuantity ToUnit(Enum unit) => unit is AirQualityIndexUnit airQualityIndexUnit
            ? this
            : throw new ArgumentException($"Must be of type {nameof(AirQualityIndexUnit)}.", nameof(unit));

        public IQuantity ToUnit(UnitSystem unitSystem) =>
            throw new NotImplementedException();

        public override string ToString() =>
            ToString(CultureInfo.CurrentCulture);

        public string ToString(string? format, IFormatProvider? formatProvider) =>
            Value.ToString(format, formatProvider);

        public string ToString(IFormatProvider? provider) =>
            ToString("N0", provider);

        string IQuantity.ToString(IFormatProvider? provider, int significantDigitsAfterRadix) =>
            throw new NotImplementedException();

        string IQuantity.ToString(IFormatProvider? provider, string format, params object[] args) =>
            throw new NotImplementedException();

        #endregion
    }
}
