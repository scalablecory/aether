using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal readonly struct Measurement
    {
        public Measure Measure { get; }
        public IQuantity Value { get; }

        public Measurement(Measure measure, IQuantity value)
        {
            Measure = measure;
            Value = value;
        }

        public override string ToString() =>
            $"{{ {Measure}: {Value} }}";
    }
}
