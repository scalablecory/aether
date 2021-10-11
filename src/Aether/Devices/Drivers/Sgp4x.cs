using System.Device.I2c;
using UnitsNet;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// Air Quality and Gas VOC Sensor SGP4x
    /// </summary>
    public sealed class Sgp4x : System.IDisposable
    {
        /// <summary>
        /// The default I²C address of this device.
        /// </summary>
        public const int DefaultI2cAddress = 0x59;

        private readonly I2cDevice _device;

        /// <summary>
        /// Instantiates a new <see cref="Sgp4x"/>.
        /// </summary>
        /// <param name="device">The I²C device to operate on.</param>
        public Sgp4x(I2cDevice device)
        {
            _device = device;
            Reset();
        }

        /// <inheritdoc/>
        public void Dispose() =>
            _device.Dispose();

        /// <summary>
        /// Resets the device.
        /// </summary>
        public void Reset()
        {
            _device.WriteByte(0x00);
            _device.WriteByte(0x06);
            Thread.Sleep(1);
        }

        /// <summary>
        /// Gets the raw VOC measurements from the sensor.
        /// </summary>
        /// <param name="disableHotPlate">If true, the hot plate will be disabled after measurements are taken.</param>
        /// If a CRC check failed for a measurement, it will be <see langword="null"/>.
        public ushort? GetVOCRawMeasure(bool disableHotPlate = true)
        {
            Span<byte> writeBuffer = stackalloc byte[10];
            Span<byte> readBuffer = stackalloc byte[3];

            // Write start measure VOC raw
            writeBuffer[0] = 0x26;
            writeBuffer[1] = 0x0F;

            // Write default humidity value + CRC (0x80, 0x00, [CRC], 0xA2)
            Sensirion.WriteUInt16BigEndianAndCRC8(writeBuffer.Slice(2, 3), 32768);
            writeBuffer[5] = 0xA2;

            // Write default temperature value + CRC (0x66, 0x66, [CRC], 0x93)
            Sensirion.WriteUInt16BigEndianAndCRC8(writeBuffer.Slice(6, 3), 26214);
            writeBuffer[9] = 0x93;

            // Transmit command and read raw results
            _device.WriteRead(writeBuffer, readBuffer);

            // Disable hot plate if instructed
            if(disableHotPlate)
            {
                DisableHotPlate();
            }

            // Read the results and validate CRC
            return Sensirion.ReadUInt16BigEndianAndCRC8(readBuffer);
        }

        /// <summary>
        /// Disables the hot plate on the device.
        /// </summary>
        public void DisableHotPlate()
        {
            _device.WriteByte(0x36);
            _device.WriteByte(0x15);
            Thread.Sleep(1);
        }
    }
}
