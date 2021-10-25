using System.Device.Spi;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// A driver for SK9822 or APA102 addressable RGB LEDs.
    /// </summary>
    public sealed class Sk9822 : BufferedDisplayDriver
    {
        private readonly SpiDevice _device;
        private readonly byte[] _buffer;
        private readonly int _pixelBytes;
        private bool _disposed;

        /// <summary>
        /// A buffer of pixels. <see cref="Draw"/> must be called to draw the pixels to the device.
        /// </summary>
        public Span<Sk9822Pixel> Pixels =>
            MemoryMarshal.Cast<byte, Sk9822Pixel>(_buffer.AsSpan(0, _pixelBytes));

        /// <summary>
        /// Initializes a new <see cref="Sk9822"/>.
        /// </summary>
        /// <param name="device">The SPI device to use.</param>
        /// <param name="pixelCount">The number of pixels.</param>
        public Sk9822(SpiDevice device, int pixelCount)
            : base(width: pixelCount, height: 1, dpiX: 1.0f, dpiY: 1.0f)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (pixelCount <= 0) throw new ArgumentOutOfRangeException(nameof(pixelCount), pixelCount, $"{nameof(pixelCount)} must be greater than 0.");

            _device = device;

            // This protocol consists of:
            // - start frame: 32 bits, all 0.

            // draw()
            // {
            // - pixels: 32 bits per pixel, ABGR, with high three bits of A set.
            // - SK9822 reset frame: 32 bits, all 0.
            // - end frame: (pixelCount-1)/2 bits, all 0. rounded up to the byte boundary. minimum 32 bits to act as a start frame.
            // }

            // This has been adapted from Tim's Blog:
            // https://cpldcpu.wordpress.com/2014/11/30/understanding-the-apa102-superled/
            // https://cpldcpu.wordpress.com/2016/12/13/sk9822-a-clone-of-the-apa102/

            const int startFrameBytes = 4;
            int pixelBytes = pixelCount * 4;
            const int sk9822ResetFrameBytes = 4;
            int endFrameBytes = Math.Max(pixelCount - 1 + 15 / 16, startFrameBytes);

            _buffer = new byte[pixelBytes + sk9822ResetFrameBytes + endFrameBytes];
            _pixelBytes = pixelBytes;

            // write start frame.
            _device.Write(_buffer.AsSpan(0, startFrameBytes));

            // turn all LEDs off.
            Pixels.Fill(new Sk9822Pixel(0, 0, 0, 0));
            Flush();
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                Pixels.Fill(new Sk9822Pixel(0, 0, 0, 0));
                Flush();

                _device.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Draws the current <see cref="Pixels"/> buffer to the device.
        /// </summary>
        public override void Flush() =>
            _device.Write(_buffer);

        public override Image CreateImage(int width, int height) =>
            new Image<Rgba32>(width, height);

        protected override void DrawImageCore(Image srcImage, int fillPositionX, int fillPositionY, DrawOrientation orientation)
        {
            if (srcImage is not Image<Rgba32> img)
            {
                throw new ArgumentException($"{nameof(srcImage)} is of an invalid type; {nameof(DisplayImage)} must be called with images created from {nameof(CreateImage)}.", nameof(srcImage));
            }

            if (!img.TryGetSinglePixelSpan(out Span<Rgba32> srcPixels))
            {
                throw new Exception("Expected contiguous buffer from ImageSharp.");
            }

            int length = srcPixels.Length;

            Span<Sk9822Pixel> dstPixels = Pixels.Slice(fillPositionX, length);

            for (int i = 0; i < length; ++i)
            {
                Rgba32 src = srcPixels[i];

                int total = src.R + src.G + src.B;
                if (total > 255)
                {
                    // These LEDs are not sRGB. This is just me fudging with things...
                    // They really need proper calibration.

                    // If total LED brightness would be over 255, normalize it down to 255.
                    // This seems to help with color uniformity without causing significant brightness shift.

                    src.R = (byte)(src.R * 255 / total);
                    src.G = (byte)(src.G * 255 / total);
                    src.B = (byte)(src.B * 255 / total);
                }

                dstPixels[i] = new Sk9822Pixel(src.A >> 3, src.R, src.G, src.B);
            }
        }
    }
}
