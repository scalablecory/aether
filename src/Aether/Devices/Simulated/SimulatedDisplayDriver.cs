using Aether.Devices.Drivers;
using SixLabors.ImageSharp;
using System.Globalization;

namespace Aether.Devices.Simulated
{
    internal sealed class SimulatedDisplayDriver : DisplayDriver
    {
        private readonly string _imageDirectoryPath;
        private int _counter;

        public override int Width { get; }

        public override int Height { get; }

        public override float DpiX { get; }

        public override float DpiY { get; }

        public SimulatedDisplayDriver(string imageDirectoryPath, int width, int height, float dpiX, float dpiY)
        {
            _imageDirectoryPath = Directory.CreateDirectory(imageDirectoryPath).FullName;
            Width = width;
            Height = height;
            DpiX = dpiX;
            DpiY = dpiY;
        }

        protected override Image CreateImageCore(int width, int height) =>
            new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(width, height);

        public override void DisplayImage(Image image, DrawOrientation orientation = DrawOrientation.Default)
        {
            int imageId = Interlocked.Increment(ref _counter);
            string filePath = string.Create(CultureInfo.InvariantCulture, $"image-{imageId}.png");
            filePath = Path.Combine(_imageDirectoryPath, filePath);

            image.SaveAsPng(filePath);
        }

        public override void Dispose()
        {
        }
    }
}
