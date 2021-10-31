using Aether.Devices.Drivers;
using SixLabors.ImageSharp;
using System.Globalization;

namespace Aether.Devices.Simulated
{
    internal sealed class SimulatedDisplayDriver : DisplayDriver
    {
        private readonly string _imageDirectoryPath;
        private int _counter;

        public SimulatedDisplayDriver(string imageDirectoryPath, int width, int height, float dpiX, float dpiY)
            : base(width, height, dpiX, dpiY)
        {
            _imageDirectoryPath = Directory.CreateDirectory(imageDirectoryPath).FullName;
        }

        public override Image CreateImage(int width, int height) =>
            new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(width, height);

        protected override void DisplayImageCore(Image image, DrawOrientation orientation)
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
