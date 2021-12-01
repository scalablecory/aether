namespace Aether.Devices.Drivers
{
    public abstract class AddressableRgbDriver
    {
        public int LedCount { get; }

        protected AddressableRgbDriver(int ledCount)
        {
            LedCount = ledCount;
        }

        public abstract void SetLeds(ReadOnlySpan<LedPixel> pixels);
    }
}
