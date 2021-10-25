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
        /// <param name="srcImage">The <see cref="Image"/> to draw onto the pixel buffer. Must have been created via <see cref="DisplayDriver.CreateImage(DrawOrientation)"/> on this instance.</param>
        /// <param name="fillPositionX">The X position to draw the image at.</param>
        /// <param name="fillPositionY">The Y position to draw the image at.</param>
        /// <param name="orientation">The orientation to draw the image in.</param>
        public void DrawImage(Image srcImage, int fillPositionX, int fillPositionY, DrawOrientation orientation = DrawOrientation.Default)
        {
            (int w, int h) = orientation switch
            {
                DrawOrientation.Default => (srcImage.Width, srcImage.Height),
                DrawOrientation.Rotate90 => (srcImage.Height, srcImage.Width),
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, $"{nameof(orientation)} is not a valid {nameof(DrawOrientation)} value.")
            };

            if (Width - fillPositionX < w || Height - fillPositionY < h)
            {
                throw new ArgumentException($"{nameof(srcImage)} is of an invalid size for this orientation; {nameof(DisplayImage)} must be called with images created from {nameof(CreateImage)}.", nameof(srcImage));
            }

            DrawImageCore(srcImage, fillPositionX, fillPositionY, orientation);
        }

        /// <inheritdoc cref="DrawImage(Image, int, int, DrawOrientation)"/>
        protected abstract void DrawImageCore(Image srcImage, int fillPositionX, int fillPositionY, DrawOrientation orientation);

        /// <summary>
        /// Flushes the pixel buffer to the device.
        /// </summary>
        public abstract void Flush();

        protected sealed override void DisplayImageCore(Image image, DrawOrientation orientation)
        {
            DrawImageCore(image, fillPositionX: 0, fillPositionY: 0, orientation);
            Flush();
        }
    }
}
