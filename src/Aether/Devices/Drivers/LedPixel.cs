namespace Aether.Devices.Drivers
{
    /// <summary>
    /// An abstract pixel, created from <see cref="AddressableRgbDriver.CreatePixelColor(in SixLabors.ImageSharp.ColorSpaces.LinearRgb, float)"/>.
    /// </summary>
    /// <param name="Brightness">The pixel's global brightness.</param>
    /// <param name="R">The pixel's red component.</param>
    /// <param name="G">The pixel's green component.</param>
    /// <param name="B">The pixel's blue component.</param>
    public readonly record struct LedPixel(byte Brightness, byte R, byte G, byte B);
}
