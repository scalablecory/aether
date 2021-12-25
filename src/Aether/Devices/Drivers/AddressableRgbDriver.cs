namespace Aether.Devices.Drivers
{
    public abstract class AddressableRgbDriver
    {
        public int LedCount { get; }

        protected AddressableRgbDriver(int ledCount)
        {
            LedCount = ledCount;
        }

        public abstract void Draw<TRenderer>(ref TRenderer renderer)
            where TRenderer : struct, IRgbPixelRenderer;
    }
}
