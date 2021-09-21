using SixLabors.ImageSharp;

namespace Aether.Devices.Drivers
{
    internal abstract class BufferedDisplayDriver : DisplayDriver
    {
        public abstract int PositionGranularityX { get; }
        public abstract int PositionGranularityY { get; }

        public sealed override void DisplayImage(Image image, DrawOrientation orientation = DrawOrientation.Landscape)
        {
            if (image.Width != Width || image.Height != Height)
            {
                throw new ArgumentException($"{nameof(image)} must be {Width}x{Height} pixels in size.");
            }

            DrawImage(image);
            DisplayBuffer();
        }

        public sealed override Image CreateImage() => CreateImage(Width, Height);
        public abstract Image CreateImage(int width, int height);

        public void DrawImage(Image image, Point position = default, DrawOrientation orientation = DrawOrientation.Landscape)
        {
            (int x, int y) = orientation switch
            {
                DrawOrientation.Landscape => (position.X, position.Y),
                DrawOrientation.Portrait => (position.Y, position.X),
                _ => throw new ArgumentOutOfRangeException(nameof(orientation))
            };

            if (x < 0 || y < 0 || Width - x < image.Width || Height - y <= image.Height)
            {
                throw new Exception($"{nameof(image)} would be drawn out of bounds at the given position.");
            }

            if ((x % PositionGranularityX) != 0 || (y % PositionGranularityY) != 0)
            {
                throw new Exception($"{nameof(position)} does not meet the {PositionGranularityX}x{PositionGranularityY} granularity requirement of {GetType().Name}.");
            }

            DrawImageCore(image, position, orientation);
        }

        protected abstract void DrawImageCore(Image image, Point position, DrawOrientation orientation);
        public abstract void DisplayBuffer();
    }
}
