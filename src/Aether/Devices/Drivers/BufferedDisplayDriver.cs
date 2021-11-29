using SixLabors.ImageSharp;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// A <see cref="DisplayDriver"/> that has a writable pixel buffer.
    /// </summary>
    public abstract class BufferedDisplayDriver : DisplayDriver
    {
        /// <summary>
        /// Initializes a new <see cref="BufferedDisplayDriver"/>.
        /// </summary>
        /// <param name="width">The width of the display.</param>
        /// <param name="height">The height of the display.</param>
        /// <param name="dpiX">The display's X DPI.</param>
        /// <param name="dpiY">The display's Y DPI.</param>
        protected BufferedDisplayDriver(int width, int height, float dpiX, float dpiY)
            : base(width, height, dpiX, dpiY)
        {
        }

        /// <summary>
        /// Draws an image onto the pixel buffer.
        /// </summary>
        /// <param name="srcImage">The <see cref="Image"/> to draw onto the pixel buffer. Must have been created via <see cref="DisplayDriver.CreateImage()"/> on this instance.</param>
        /// <param name="fillPositionX">The X position to draw the image at.</param>
        /// <param name="fillPositionY">The Y position to draw the image at.</param>
        /// <param name="options">Options controlling how the image is drawn.</param>
        public void DrawImage(Image srcImage, int fillPositionX, int fillPositionY, DrawOptions options = DrawOptions.None)
        {
            (int w, int h) = options.HasFlag(DrawOptions.Rotate90)
                ? (srcImage.Height, srcImage.Width)
                : (srcImage.Width, srcImage.Height);

            if (Width - fillPositionX < w || Height - fillPositionY < h)
            {
                throw new ArgumentException($"{nameof(srcImage)} is of an invalid size for this orientation; {nameof(DisplayImage)} must be called with images created from {nameof(CreateImage)}.", nameof(srcImage));
            }

            DrawImageCore(srcImage, fillPositionX, fillPositionY, options);
        }

        /// <inheritdoc cref="DrawImage(Image, int, int, DrawOptions)"/>
        protected abstract void DrawImageCore(Image srcImage, int fillPositionX, int fillPositionY, DrawOptions options);

        /// <summary>
        /// Flushes the pixel buffer to the device.
        /// </summary>
        public abstract void Flush();

        protected sealed override void DisplayImageCore(Image image, DrawOptions options)
        {
            DrawImageCore(image, fillPositionX: 0, fillPositionY: 0, options);
            Flush();
        }
    }
}
