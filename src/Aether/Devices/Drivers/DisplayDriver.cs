using SixLabors.ImageSharp;

namespace Aether.Devices.Drivers
{
    internal abstract class DisplayDriver : IDisposable
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract float DpiX { get; }
        public abstract float DpiY { get; }

        public Image CreateImage(DrawOrientation orientation = DrawOrientation.Default)
        {
            (int width, int height) = orientation switch
            {
                DrawOrientation.Default => (Width, Height),
                DrawOrientation.Rotate90 => (Height, Width),
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), $"{nameof(orientation)} is not a valid {nameof(DrawOrientation)} value.")
            };

            return CreateImageCore(width, height);
        }

        protected abstract Image CreateImageCore(int width, int height);

        public abstract void DisplayImage(Image image, DrawOrientation orientation = DrawOrientation.Default);

        public abstract void Dispose();
    }
}
