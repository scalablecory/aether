using UnitsNet;

namespace Aether.CustomUnits
{
	public enum VolatileOrganicCompoundIndexUnit
    {
		IndexValue
	}

    public struct VolatileOrganicCompoundIndex : IQuantity
    {
		public VolatileOrganicCompoundIndex(double value, VolatileOrganicCompoundIndexUnit unit = VolatileOrganicCompoundIndexUnit.IndexValue)
		{
			Unit = unit;
			Value = value;
		}

		Enum IQuantity.Unit => Unit;
		public VolatileOrganicCompoundIndexUnit Unit { get; }

		public double Value { get; }

		#region IQuantity

		private static readonly VolatileOrganicCompoundIndex Zero = new VolatileOrganicCompoundIndex(0, VolatileOrganicCompoundIndexUnit.IndexValue);

		public QuantityType Type => QuantityType.Information;
		public BaseDimensions Dimensions => BaseDimensions.Dimensionless;

		public QuantityInfo QuantityInfo => new QuantityInfo("IndexUnit", typeof(VolatileOrganicCompoundIndexUnit),
			new UnitInfo[]
			{
				new UnitInfo<VolatileOrganicCompoundIndexUnit>(VolatileOrganicCompoundIndexUnit.IndexValue, BaseUnits.Undefined)
			},
			VolatileOrganicCompoundIndexUnit.IndexValue,
			Zero,
			BaseDimensions.Dimensionless);

		public double As(Enum unit) => Convert.ToDouble(unit);

		public double As(UnitSystem unitSystem) => throw new NotImplementedException();

		public IQuantity ToUnit(Enum unit)
		{
			if (unit is VolatileOrganicCompoundIndexUnit howMuchUnit) return new VolatileOrganicCompoundIndex(As(unit), howMuchUnit);
			throw new ArgumentException("Must be of type VolatileOrganicCompoundIndexUnit.", nameof(unit));
		}

		public IQuantity ToUnit(UnitSystem unitSystem) => throw new NotImplementedException();

		public override string ToString() => $"{Value} {UnitAbbreviationsCache.Default.GetDefaultAbbreviation(Unit)}";
		public string ToString(string? format, IFormatProvider? formatProvider) => $"VolatileOrganicCompoundIndex ({format}, {formatProvider})";
		public string ToString(IFormatProvider? provider) => $"VolatileOrganicCompoundIndex ({provider})";
		public string ToString(IFormatProvider? provider, int significantDigitsAfterRadix) => $"VolatileOrganicCompoundIndex ({provider}, {significantDigitsAfterRadix})";
		public string ToString(IFormatProvider? provider, string format, params object[] args) => $"VolatileOrganicCompoundIndex ({provider}, {string.Join(", ", args)})";

		#endregion
	}
}
