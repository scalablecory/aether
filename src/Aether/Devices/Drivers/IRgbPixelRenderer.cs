namespace Aether.Devices.Drivers
{
    /// <summary>
    /// A renderer for RGB pixels.
    /// </summary>
    public interface IRgbPixelRenderer
    {
        /// <summary>
        /// Renders against a device framebuffer.
        /// </summary>
        /// <typeparam name="TPixel">The type of pixel to render.</typeparam>
        /// <param name="pixels">A span of the framebuffer to render to.</param>
        void Render<TPixel>(Span<TPixel> pixels)
            where TPixel : struct, IRgbPixelFactory<TPixel>;
    }
}
