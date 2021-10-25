using System.Runtime.InteropServices;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// <para>
    /// A blittable pixel format used by the <see cref="Sk9822"/> driver for SK9822 or APA102 addressable RGB LEDs.
    /// </para>
    /// <para>
    /// The <see cref="Sk9822Pixel"/> has a memory layout of 32-bit ABGR, with A being a 5-bit value in form:
    /// </para>
    /// <code>0b111x_xxxx</code>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Sk9822Pixel
    {
        private const int BrightnessMask = 0b1110_0000;
        private const uint MaxBrightness = ~(uint)BrightnessMask;

        private byte _a;
        private byte _b;
        private byte _g;
        private byte _r;

        /// <summary>
        /// Initializes a new <see cref="Sk9822Pixel"/>.
        /// </summary>
        /// <param name="brightness">The global brightness of the pixel. Must be between 0 and 31.</param>
        /// <param name="r">The brightness of the red component of the pixel.</param>
        /// <param name="g">The brightness of the green component of the pixel.</param>
        /// <param name="b">The brightness of the blue component of the pixel.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="brightness"/> is not between 0 and 31.</exception>
        public Sk9822Pixel(int brightness, byte r, byte g, byte b)
        {
            if ((uint)brightness > MaxBrightness)
            {
                ThrowArgumentOutOfRange(brightness);

                static void ThrowArgumentOutOfRange(int brightness) =>
                    throw new ArgumentOutOfRangeException(nameof(brightness), brightness, $"{nameof(brightness)} must be between 0 and {MaxBrightness}.");
            }

            _a = (byte)(brightness | BrightnessMask);
            _b = b;
            _g = g;
            _r = r;
        }

        /// <summary>
        /// The global brightness of the pixel. Must be between 0 and 31.
        /// </summary>
        public int Brightness
        {
            readonly get => _a & ~BrightnessMask;
            set
            {
                if ((uint)value > MaxBrightness)
                {
                    ThrowArgumentOutOfRange(value);

                    static void ThrowArgumentOutOfRange(int value) =>
                        throw new ArgumentOutOfRangeException(nameof(Brightness), value, $"{nameof(Brightness)} must be between 0 and {MaxBrightness}.");
                }

                _a = (byte)(value | BrightnessMask);
            }
        }

        /// <summary>
        /// The brightness of the red component of the pixel.
        /// </summary>
        public byte Red
        {
            readonly get => _r;
            set => _r = value;
        }

        /// <summary>
        /// The brightness of the green component of the pixel.
        /// </summary>
        public byte Green
        {
            readonly get => _g;
            set => _g = value;
        }

        /// <summary>
        /// The brightness of the blue component of the pixel.
        /// </summary>
        public byte Blue
        {
            readonly get => _b;
            set => _b = value;
        }
    }
}
