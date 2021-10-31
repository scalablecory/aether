using SixLabors.ImageSharp;

namespace Aether.Devices.Drivers
{
    public abstract class DisplayDriver : IDisposable
    {
        /// <summary>
        /// The display's width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The display's height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The display's X DPI.
        /// </summary>
        public float DpiX { get; }

        /// <summary>
        /// The display's Y DPI.
        /// </summary>
        public float DpiY { get; }

        /// <summary>
        /// Initializes a new <see cref="DisplayDriver"/>.
        /// </summary>
        /// <param name="width">The width of the display.</param>
        /// <param name="height">The height of the display.</param>
        /// <param name="dpiX">The display's X DPI.</param>
        /// <param name="dpiY">The display's Y DPI.</param>
        protected DisplayDriver(int width, int height, float dpiX, float dpiY)
        {
            Width = width;
            Height = height;
            DpiX = dpiX;
            DpiY = dpiY;
        }

        /// <summary>
        /// Creates an image that can be displayed by this driver.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <returns>A new <see cref="Image"/> that can be displayed by this <see cref="DisplayDriver"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public abstract Image CreateImage(int width, int height);

        /// <summary>
        /// Displays an image to the device.
        /// </summary>
        /// <param name="image">The image to display. Must have been created via <see cref="CreateImage(int, int)"/> on this <see cref="DisplayDriver"/>.</param>
        /// <param name="orientation">The orientation to display the image in</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void DisplayImage(Image image, DrawOrientation orientation = DrawOrientation.Default)
        {
            (int width, int height) = orientation switch
            {
                DrawOrientation.Default => (Width, Height),
                DrawOrientation.Rotate90 => (Height, Width),
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), $"{nameof(orientation)} is not a valid {nameof(DrawOrientation)} value.")
            };

            if (image.Width != width || image.Height != height)
            {
                throw new ArgumentException($"{nameof(image)} is of an invalid size for this orientation; {nameof(DisplayImage)} must be called with images created from {nameof(CreateImage)}.", nameof(image));
            }

            DisplayImageCore(image, orientation);
        }

        /// <inheritdoc cref="DisplayImage(Image, DrawOrientation)"/>
        protected abstract void DisplayImageCore(Image image, DrawOrientation orientation);

        public abstract void Dispose();
    }
}
