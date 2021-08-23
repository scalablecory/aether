namespace Aether.Devices.Sensors.Metadata
{
    internal sealed class MeasureInfo
    {
        public Measure Measure { get; }

        public MeasureInfo(Measure measure)
        {
            Measure = measure;
        }
    }
}
