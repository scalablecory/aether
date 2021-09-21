using SixLabors.ImageSharp;
using System.Buffers.Binary;
using System.Device.Gpio;
using System.Device.Spi;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using L8 = SixLabors.ImageSharp.PixelFormats.L8;

namespace Aether.Devices.Drivers
{
    internal sealed class WaveshareEPD2_9inV2 : BufferedDisplayDriver
    {
        private static ReadOnlySpan<byte> s_driverOutputControlBytes => new byte[] { 0x27, 0x01, 0x00 };
        private static ReadOnlySpan<byte> s_dataEntryModeBytes => new byte[] { 0x03 };
        private static ReadOnlySpan<byte> s_displayUpdateControlBytes => new byte[] { 0x00, 0x80 };

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

        public override int Width => 296;
        public override int Height => 128;
        private int BitsPerImage => Width * Height;
        private int BytesPerImage => BitsPerImage / 8;

        public override int PositionGranularityX => 8;

        public override int PositionGranularityY => 1;

        public override float DpiX => 123;

        public override float DpiY => 456;

        public WaveshareEPD2_9inV2(SpiDevice device, GpioController gpio, int dcPinId, int rstPinId, int busyPinId)
        {
            _device = device;
            _gpio = gpio;
            _dcPinId = dcPinId;
            _rstPinId = rstPinId;
            _busyPinId = busyPinId;
            _imageBuffer = new byte[BytesPerImage];
        }

        public override void Dispose() =>
            _device.Dispose();

        public override Image CreateImage(int width, int height) =>
            new Image<L8>(width, height);

        protected override void DrawImageCore(Image image, Point position, DrawOrientation orientation)
        {
            var img = (Image<L8>)image;
            int width, height;

            if (orientation == DrawOrientation.Landscape)
            {
                ConvertTo1bpp(_imageBuffer, img);
                width = image.Width;
                height = image.Height;
            }
            else
            {
                ConvertTo1bppRotated(_imageBuffer, img);
                position = new Point(position.Y, position.X);
                width = image.Height;
                height = image.Width;
            }

            SetDisplayWindow((uint)position.X, (uint)position.Y, (uint)(position.X + width), (uint)(position.Y + height));
            SetImage(_imageBuffer.AsSpan(image.Width * image.Height / 8));
        }

        private static void ConvertTo1bpp(Span<byte> dest, Image<L8> src)
        {
            ref byte rdest = ref MemoryMarshal.GetReference(dest);

            for (int y = 0; y < src.Height; ++y)
            {
                Span<L8> row = src.GetPixelRowSpan(y);
                
                Debug.Assert(row.Length == src.Width);
                Debug.Assert((row.Length % 8) == 0);

                ref sbyte rsrc = ref Unsafe.As<L8, sbyte>(ref MemoryMarshal.GetReference(row));
                ref sbyte rsrcEnd = ref Unsafe.Add(ref rsrc, row.Length);

                do
                {
                    rdest =
                        (byte)(
                        (0b10000000 & ~(rsrc >> 7)) |
                        (0b01000000 & ~(Unsafe.Add(ref rsrc, 1) >> 7)) |
                        (0b00100000 & ~(Unsafe.Add(ref rsrc, 2) >> 7)) |
                        (0b00010000 & ~(Unsafe.Add(ref rsrc, 3) >> 7)) |
                        (0b00001000 & ~(Unsafe.Add(ref rsrc, 4) >> 7)) |
                        (0b00000100 & ~(Unsafe.Add(ref rsrc, 5) >> 7)) |
                        (0b00000010 & ~(Unsafe.Add(ref rsrc, 6) >> 7)) |
                        (0b00000001 & ~(Unsafe.Add(ref rsrc, 7) >> 7))
                        );

                    rdest = ref Unsafe.Add(ref rdest, 1);
                    rsrc = ref Unsafe.Add(ref rsrc, 8);
                }
                while (!Unsafe.AreSame(ref rsrc, ref rsrcEnd));
            }
            
        }

        private static void ConvertTo1bppRotated(Span<byte> dest, Image<L8> src)
        {
            for (int x = 0; x < src.Width; ++x)
            {
                for (int y = 0; y < src.Height; y += 8)
                {
                    dest[(x * src.Height + y) / 8] =
                        (byte)(
                        (0b10000000 & ~((sbyte)src[x, y].PackedValue >> 7)) |
                        (0b01000000 & ~((sbyte)src[x, y + 1].PackedValue >> 7)) |
                        (0b00100000 & ~((sbyte)src[x, y + 2].PackedValue >> 7)) |
                        (0b00010000 & ~((sbyte)src[x, y + 3].PackedValue >> 7)) |
                        (0b00001000 & ~((sbyte)src[x, y + 4].PackedValue >> 7)) |
                        (0b00000100 & ~((sbyte)src[x, y + 5].PackedValue >> 7)) |
                        (0b00000010 & ~((sbyte)src[x, y + 6].PackedValue >> 7)) |
                        (0b00000001 & ~((sbyte)src[x, y + 7].PackedValue >> 7))
                        );
                }
            }
        }

        public override void DisplayBuffer() =>
            TurnOnDisplay();

        private void SetImage(ReadOnlySpan<byte> buffer) =>
            SendCommand(0x24, buffer[..BytesPerImage]);

        private void TurnOnDisplay()
        {
            // Display Update Control.
            byte data = 0xC7;
            SendCommand(0x22, MemoryMarshal.CreateReadOnlySpan(ref data, 1));

            // Activate Display Update Sequence
            SendCommand(0x20);

            ReadBusy();
        }

        private void Init()
        {
            Reset();
            Thread.Sleep(100);

            ReadBusy();

            SoftReset();

            ReadBusy();

            SetDriverOutputControl();
            SetDataEntryMode();
            SetDisplayWindow(0, 0, (uint)Width, (uint)Height);
            SetDisplayUpdateControl();
            SetCursor(0, 0);

            ReadBusy();

            SetLUTByHost(s_LUT);
        }

        private void Reset()
        {
            _gpio.Write(_rstPinId, PinValue.High);
            Thread.Sleep(10);
            _gpio.Write(_rstPinId, PinValue.Low);
            Thread.Sleep(2);
            _gpio.Write(_rstPinId, PinValue.High);
            Thread.Sleep(10);
        }

        private void SoftReset() =>
            SendCommand(0x12);

        private void SetDriverOutputControl() =>
            SendCommand(0x01, s_driverOutputControlBytes);

        private void SetDataEntryMode() =>
            SendCommand(0x11, s_dataEntryModeBytes);

        private void SetDisplayWindow(uint xStart, uint yStart, uint xEnd, uint yEnd)
        {
            Debug.Assert((xStart & 0b111) == 0, $"{nameof(xStart)} must be in multiples of 8 bits.");
            Debug.Assert((xEnd & 0b111) == 0, $"{nameof(xEnd)} must be in multiples of 8 bits.");

            Span<byte> buffer = stackalloc byte[4];

            buffer[0] = (byte)(xStart >> 3);
            buffer[1] = (byte)(xEnd >> 3);
            SendCommand(0x44, buffer[..2]);

            BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)yStart);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], (ushort)yEnd);
            SendCommand(0x45, buffer);
        }

        private void SetDisplayUpdateControl() =>
            SendCommand(0x21, s_displayUpdateControlBytes);

        private void SetCursor(uint x, uint y)
        {
            Span<byte> buffer = stackalloc byte[2];

            buffer[0] = (byte)x;
            SendCommand(0x4E, buffer[..1]);

            BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)y);
            SendCommand(0x4F, buffer);
        }

        private void SetLUTByHost(ReadOnlySpan<byte> lut)
        {
            SendCommand(0x32, lut[..153]);      // LUT
            SendCommand(0x3F, lut[153..154]);   // Unknown.
            SendCommand(0x03, lut[154..155]);   // Gate voltage.
            SendCommand(0x04, lut[155..158]);   // Source voltage. (VSH, VSH2, VSL)
            SendCommand(0x2C, lut[158..159]);   // VCOM
        }

        private void SendCommand(byte command)
        {
            _gpio.Write(_dcPinId, PinValue.Low);
            _device.WriteByte(command);
        }

        private void SendCommand(byte command, ReadOnlySpan<byte> data)
        {
            SendCommand(command);

            _gpio.Write(_dcPinId, PinValue.High);
            _device.Write(data);
        }

        private void ReadBusy()
        {
            while (_gpio.Read(_busyPinId) == PinValue.High)
            {
                Thread.Sleep(50);
            }
        }
    }
}
