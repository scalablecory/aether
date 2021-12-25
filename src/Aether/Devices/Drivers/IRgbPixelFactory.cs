using SixLabors.ImageSharp.ColorSpaces;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// Implemented by device-specific RGB pixels to create themselves from standard colorspaces.
    /// </summary>
    /// <typeparam name="TPixel">The device-specific RGB pixel type.</typeparam>
    public interface IRgbPixelFactory<TPixel>
        where TPixel : struct
    {
        /// <summary>
        /// Creates a <typeparamref name="TPixel"/> from a linear RGB value and a global brightness value.
        /// </summary>
        /// <param name="rgb">The color to create, in linear RGB.</param>
        /// <param name="brightness">A global brightness value, from 0 to 1.</param>
        /// <returns>A new <typeparamref name="TPixel"/>.</returns>
        static abstract TPixel Create(LinearRgb rgb, float brightness);
    }
}
