using SixLabors.ImageSharp;
using System.Buffers.Binary;
using System.Device.Gpio;
using System.Device.Spi;
using System.Diagnostics;
using System.Runtime.InteropServices;
using L8 = SixLabors.ImageSharp.PixelFormats.L8;

namespace Aether.Devices.Drivers
{
    internal sealed class WaveshareEPD2_9inV2 : DisplayDriver
    {
        public const int DefaultDcPin = 25;
        public const int DefaultRstPin = 17;
        public const int DefaultBusyPin = 24;

        private static ReadOnlySpan<byte> s_LUT => new byte[]
        {
            0x80,   0x66,   0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x40,   0x0,    0x0,    0x0,
            0x10,   0x66,   0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x20,   0x0,    0x0,    0x0,
            0x80,   0x66,   0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x40,   0x0,    0x0,    0x0,
            0x10,   0x66,   0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x20,   0x0,    0x0,    0x0,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x14,   0x8,    0x0,    0x0,    0x0,    0x0,    0x1,
            0xA,    0xA,    0x0,    0xA,    0xA,    0x0,    0x1,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x14,   0x8,    0x0,    0x1,    0x0,    0x0,    0x1,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x1,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x0,    0x0,    0x0,    0x0,    0x0,    0x0,    0x0,
            0x44,   0x44,   0x44,   0x44,   0x44,   0x44,   0x0,    0x0,    0x0,
            0x22,   0x17,   0x41,   0x0,    0x32,   0x36
        };

        private readonly SpiDevice _device;
        private readonly GpioController _gpio;
        private readonly int _dcPinId;
        private readonly int _rstPinId;
        private readonly int _busyPinId;
        private readonly byte[] _imageBuffer;
        private bool _disposed;

        public override int Width => 128;
        public override int Height => 296;
        private int BitsPerImage => Width * Height;
        private int BytesPerImage => BitsPerImage / 8;

        public override float DpiX => 111.917383820998f;

        public override float DpiY => 112.399461802960f;

        public WaveshareEPD2_9inV2(SpiDevice device, GpioController gpio, int dcPinId = DefaultDcPin, int rstPinId = DefaultRstPin, int busyPinId = DefaultBusyPin)
        {
            _device = device;
            _gpio = gpio;
            _dcPinId = dcPinId;
            _rstPinId = rstPinId;
            _busyPinId = busyPinId;
            _imageBuffer = new byte[BytesPerImage];

            gpio.OpenPin(dcPinId, PinMode.Output);
            gpio.OpenPin(rstPinId, PinMode.Output);
            gpio.OpenPin(busyPinId, PinMode.InputPullUp);

            Reset();
            SoftReset();

            SetDriverOutputControl();
            SetDataEntryMode();
            SetWindow(0, 0, (uint)Width - 1, (uint)Height - 1);
            SetDisplayUpdateControl();
            SetCursor(0, 0);
            SetLUTByHost(s_LUT);
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _device.Dispose();
            try
            {
                _gpio.Write(_dcPinId, PinValue.Low);
                _gpio.Write(_rstPinId, PinValue.Low);
                _gpio.Write(_busyPinId, PinValue.Low);
                _gpio.ClosePin(_dcPinId);
                _gpio.ClosePin(_rstPinId);
                _gpio.ClosePin(_busyPinId);
            }
            finally
            {
                _gpio.Dispose();
            }
        }

        protected override Image CreateImageCore(int width, int height) =>
            new Image<L8>(width, height);

        public override void DisplayImage(Image image, DrawOrientation orientation = DrawOrientation.Default)
        {
            if (image is not Image<L8> img)
            {
                throw new ArgumentException($"{nameof(image)} is of an invalid type; {nameof(DisplayImage)} must be called with images created from {nameof(CreateImage)}.", nameof(image));
            }

            switch (orientation)
            {
                case DrawOrientation.Default:
                    if (img.Width != Width || img.Height != Height)
                    {
                        throw new ArgumentException($"{nameof(image)} is of an invalid size for this orientation; {nameof(DisplayImage)} must be called with images created from {nameof(CreateImage)}.", nameof(image));
                    }
                    ConvertTo1bpp(_imageBuffer, img);
                    break;
                case DrawOrientation.Rotate90:
                    if (img.Width != Height || img.Height != Width)
                    {
                        throw new ArgumentException($"{nameof(image)} is of an invalid size for this orientation; {nameof(DisplayImage)} must be called with images created from {nameof(CreateImage)}.", nameof(image));
                    }
                    ConvertTo1bppRotated(_imageBuffer, img);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation), $"{nameof(orientation)} is not a valid {nameof(DrawOrientation)} value.");
            }

            Debug.Assert(_imageBuffer.Length == 4736);

            SetImage(_imageBuffer);
            SetPaintMode(DisplayUpdateMode.Full); // TODO: does this need to be set every draw?
            SwapFrameBuffers();
        }

        private static void ConvertTo1bpp(Span<byte> dest, Image<L8> src)
        {
            int width = src.Width;
            int height = src.Height;
            int destIdx = 0;

            for (int y = 0; y < height; ++y)
            {
                Span<L8> row = src.GetPixelRowSpan(y);

                for (int x = 0; x < width; x += 8)
                {
                    dest[destIdx++] =
                        (byte)(
                        (0b10000000 & ((sbyte)row[x].PackedValue >> 7)) |
                        (0b01000000 & ((sbyte)row[x + 1].PackedValue >> 7)) |
                        (0b00100000 & ((sbyte)row[x + 2].PackedValue >> 7)) |
                        (0b00010000 & ((sbyte)row[x + 3].PackedValue >> 7)) |
                        (0b00001000 & ((sbyte)row[x + 4].PackedValue >> 7)) |
                        (0b00000100 & ((sbyte)row[x + 5].PackedValue >> 7)) |
                        (0b00000010 & ((sbyte)row[x + 6].PackedValue >> 7)) |
                        (0b00000001 & ((sbyte)row[x + 7].PackedValue >> 7))
                        );
                }
            }
        }

        private static void ConvertTo1bppRotated(Span<byte> dest, Image<L8> src)
        {
            int width = src.Width;
            int height = src.Height;
            int destIdx = 0;

            for (int y = 0; y < height; y += 8)
            {
                Span<L8> row0 = src.GetPixelRowSpan(y);
                Span<L8> row1 = src.GetPixelRowSpan(y + 1);
                Span<L8> row2 = src.GetPixelRowSpan(y + 2);
                Span<L8> row3 = src.GetPixelRowSpan(y + 3);
                Span<L8> row4 = src.GetPixelRowSpan(y + 4);
                Span<L8> row5 = src.GetPixelRowSpan(y + 5);
                Span<L8> row6 = src.GetPixelRowSpan(y + 6);
                Span<L8> row7 = src.GetPixelRowSpan(y + 7);

                for (int x = 0; x < width; ++x)
                {
                    dest[destIdx++] =
                        (byte)(
                        (0b10000000 & ((sbyte)row0[x].PackedValue >> 7)) |
                        (0b01000000 & ((sbyte)row1[x].PackedValue >> 7)) |
                        (0b00100000 & ((sbyte)row2[x].PackedValue >> 7)) |
                        (0b00010000 & ((sbyte)row3[x].PackedValue >> 7)) |
                        (0b00001000 & ((sbyte)row4[x].PackedValue >> 7)) |
                        (0b00000100 & ((sbyte)row5[x].PackedValue >> 7)) |
                        (0b00000010 & ((sbyte)row6[x].PackedValue >> 7)) |
                        (0b00000001 & ((sbyte)row7[x].PackedValue >> 7))
                        );
                }
            }
        }

        private void SetPaintMode(DisplayUpdateMode mode) =>
            SendCommand(0x22, (byte)mode);

        private void SwapFrameBuffers()
        {
            SendCommand(0x20);
            ReadBusy();
        }

        private void Reset()
        {
            _gpio.Write(_rstPinId, PinValue.High);
            Thread.Sleep(10);
            _gpio.Write(_rstPinId, PinValue.Low);
            Thread.Sleep(2);
            _gpio.Write(_rstPinId, PinValue.High);
            Thread.Sleep(10);
            ReadBusy();
        }

        private void SoftReset()
        {
            SendCommand(0x12);
            ReadBusy();
        }

        private void SetDriverOutputControl() =>
            SendCommand(0x01, new byte[] { 0x27, 0x01, 0x00 });

        private void SetDataEntryMode() =>
            SendCommand(0x11, 0x03);

        /// <summary>
        /// Sets the window into the device's frame buffer to operate to.
        /// </summary>
        /// <param name="xStart">The starting X coordinate. Must be a multiple of 8.</param>
        /// <param name="yStart">The starting Y coordinate.</param>
        /// <param name="xEnd">The ending X coordinate, inclusive. Must be a multiple of 8.</param>
        /// <param name="yEnd">The ending Y coordinate, inclusive.</param>
        private void SetWindow(uint xStart, uint yStart, uint xEnd, uint yEnd)
        {
            Debug.Assert((xStart & 0b111) == 0, $"{nameof(xStart)} must be in multiples of 8.");
            Debug.Assert(xStart < Width);
            Debug.Assert(xEnd < Width);
            Debug.Assert(yStart < Height);
            Debug.Assert(yEnd < Height);

            Span<byte> buffer = stackalloc byte[4];

            buffer[0] = (byte)(xStart >> 3);
            buffer[1] = (byte)(xEnd >> 3);
            SendCommand(0x44, buffer[..2]);

            BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)yStart);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], (ushort)yEnd);
            SendCommand(0x45, buffer);
        }

        /// <summary>
        /// Sets the cursor to write data to.
        /// </summary>
        /// <param name="x">The X coordinate. Must be a multiple of 8.</param>
        /// <param name="y">The Y coordinate.</param>
        private void SetCursor(uint x, uint y)
        {
            Debug.Assert((x & 0b111) == 0, $"{nameof(x)} must be in multiples of 8.");
            Debug.Assert(x < Width);
            Debug.Assert(y < Height);

            SendCommand(0x4E, (byte)(x >> 3));

            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)y);
            SendCommand(0x4F, buffer);

            ReadBusy();
        }

        /// <summary>
        /// Fills the current window with image data starting from the current cursor.
        /// </summary>
        private void SetImage(ReadOnlySpan<byte> buffer)
        {
            Debug.Assert(buffer.Length == BytesPerImage);
            SendCommand(0x24, buffer);
        }

        private void SetDisplayUpdateControl() =>
            SendCommand(0x21, new byte[] { 0x00, 0x80 });

        private void SetLUTByHost(ReadOnlySpan<byte> lut)
        {
            SendCommand(0x32, lut[..153]);      // LUT
            ReadBusy();

            SendCommand(0x3F, lut[153..154]);   // Unknown.
            SendCommand(0x03, lut[154..155]);   // Gate voltage.
            SendCommand(0x04, lut[155..158]);   // Source voltage. (VSH, VSH2, VSL)
            SendCommand(0x2C, lut[158..159]);   // VCOM
        }

        /// <summary>
        /// Sends a command with no data.
        /// </summary>
        private void SendCommand(byte command)
        {
            _gpio.Write(_dcPinId, PinValue.Low);
            _device.WriteByte(command);
        }

        /// <summary>
        /// Sends a command with data.
        /// </summary>
        private void SendCommand(byte command, byte data) =>
            SendCommand(command, MemoryMarshal.CreateReadOnlySpan(ref data, 1));

        /// <summary>
        /// Sends a command with data.
        /// </summary>
        private void SendCommand(byte command, ReadOnlySpan<byte> data)
        {
            Debug.Assert(data.Length != 0, "Call dataless overload instead.");

            SendCommand(command);

            _gpio.Write(_dcPinId, PinValue.High);
            foreach(byte b in data)
            {
                _device.WriteByte(b);
            }
        }

        /// <summary>
        /// Waits for the busy pin to go low.
        /// </summary>
        private void ReadBusy()
        {
            bool busy;
            do
            {
                busy = _gpio.Read(_busyPinId) == PinValue.High;
                Thread.Sleep(50);
            }
            while (busy);
        }

        private enum DisplayUpdateMode : byte
        {
            Full = 0xC7,
            Partial = 0x0F
        }
    }
}
