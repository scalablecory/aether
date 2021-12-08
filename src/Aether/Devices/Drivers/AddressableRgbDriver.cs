using SixLabors.ImageSharp.ColorSpaces;

namespace Aether.Devices.Drivers
{
    public abstract class AddressableRgbDriver
    {
        public int LedCount { get; }

        protected AddressableRgbDriver(int ledCount)
        {
            LedCount = ledCount;
        }

        /// <summary>
        /// Converts a linear RGB value and a global brightness into a <see cref="LedPixel"/>.
        /// </summary>
        /// <param name="rgb">The color to create, in linear RGB.</param>
        /// <param name="brightness">A global brightness value, from 0 to 1.</param>
        /// <returns></returns>
        public abstract LedPixel CreatePixelColor(in LinearRgb rgb, float brightness);

        public abstract void SetLeds(ReadOnlySpan<LedPixel> pixels);
    }
}
