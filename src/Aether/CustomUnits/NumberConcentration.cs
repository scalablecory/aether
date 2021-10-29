using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Aether.CustomUnits
{
    public enum NumberConcentrationUnit
    {
        ParticulatePerCubicCentimeter
    }

    public struct NumberConcentration : IQuantity
    {
        public NumberConcentration(double value, NumberConcentrationUnit unit = NumberConcentrationUnit.ParticulatePerCubicCentimeter)
        {
            Unit = unit;
            Value = value;
        }

        Enum IQuantity.Unit => Unit;
        public NumberConcentrationUnit Unit { get; }

        public double Value { get; }

        #region IQuantity

        private static readonly NumberConcentration Zero = new NumberConcentration(0, NumberConcentrationUnit.ParticulatePerCubicCentimeter);

        public QuantityType Type => QuantityType.Information;
        public BaseDimensions Dimensions => BaseDimensions.Dimensionless;

        public QuantityInfo QuantityInfo => new QuantityInfo("NumberConcentrationUnit", typeof(NumberConcentrationUnit),
            new UnitInfo[]
            {
                new UnitInfo<NumberConcentrationUnit>(NumberConcentrationUnit.ParticulatePerCubicCentimeter, BaseUnits.Undefined)
            },
            NumberConcentrationUnit.ParticulatePerCubicCentimeter,
            Zero,
            BaseDimensions.Dimensionless);

        public double As(Enum unit) => Convert.ToDouble(unit);

        public double As(UnitSystem unitSystem) => throw new NotImplementedException();

        public IQuantity ToUnit(Enum unit)
        {
            if (unit is NumberConcentrationUnit numberConcentrationUnit)
                return new NumberConcentration(As(unit), numberConcentrationUnit);
            throw new ArgumentException("Must be of type NumberConcentrationUnit.", nameof(unit));
        }

        public IQuantity ToUnit(UnitSystem unitSystem) => throw new NotImplementedException();

        public override string ToString() => $"{string.Format("{0:0.00}", Value)}/cm³";
        public string ToString(string? format, IFormatProvider? formatProvider) => $"Number Concentration ({format}, {formatProvider})";
        public string ToString(IFormatProvider? provider) => $"Number Concentration ({provider})";
        public string ToString(IFormatProvider? provider, int significantDigitsAfterRadix) => $"Number Concentration ({provider}, {significantDigitsAfterRadix})";
        public string ToString(IFormatProvider? provider, string format, params object[] args) => $"Number Concentration ({provider}, {string.Join(", ", args)})";

        #endregion
    }
}
