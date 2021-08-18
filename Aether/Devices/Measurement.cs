namespace Aether.Devices
{
    internal readonly struct Measurement
    {
        public Measure Measure { get; }
        public float Value { get; }

        public Measurement(Measure measure, float value)
        {
            Measure = measure;
            Value = value;
        }
    }
}
