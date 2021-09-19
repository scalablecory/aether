using System.Buffers.Binary;
using System.Device.Gpio;
using System.Device.Spi;
using System.Diagnostics;

namespace Aether.Devices.Drivers
{
    internal sealed class WaveshareEPD2_9inV2 : System.IDisposable
    {
        public const int Width = 128;
        public const int Height = 296;
        public const int BitsPerImage = Width * Height;
        public const int BytesPerImage = BitsPerImage / 8;

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

        public WaveshareEPD2_9inV2(SpiDevice device, GpioController gpio, int dcPinId, int rstPinId, int busyPinId)
        {
            _device = device;
            _gpio = gpio;
            _dcPinId = dcPinId;
            _rstPinId = rstPinId;
            _busyPinId = busyPinId;
        }

        public void Dispose() =>
            _device.Dispose();

        public void SetImage(ReadOnlySpan<byte> buffer) =>
            SendCommand(0x24, buffer[..BytesPerImage]);

        private void Init()
        {
            Reset();
            Thread.Sleep(100);

            ReadBusy();

            SoftReset();

            ReadBusy();

            SetDriverOutputControl();
            SetDataEntryMode();
            SetDisplayWindow(0, 0, Width, Height);
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
