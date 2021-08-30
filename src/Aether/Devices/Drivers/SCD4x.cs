using System.Buffers.Binary;
using System.Device.I2c;
using UnitsNet;

namespace Aether.Devices.Drivers
{
    /// <summary>
    /// A driver for Sensirion's SCD4x.
    /// </summary>
    public sealed class SCD4x : IDisposable
    {
        private static ReadOnlySpan<byte> StartPeriodicMeasurementBytes => new byte[] { 0x21, 0xB1 };
        private static ReadOnlySpan<byte> CheckDataReadyStatusBytes => new byte[] { 0xE4, 0xB8 };
        private static ReadOnlySpan<byte> ReadPeriodicMeasurementBytes => new byte[] { 0xEC, 0x05 };
        private static ReadOnlySpan<byte> StopPeriodicMeasurementBytes => new byte[] { 0x3F, 0x86 };

        private readonly I2cDevice _device;

        /// <summary>
        /// Instantiates a new <see cref="SCD4x"/>.
        /// </summary>
        /// <param name="device">The I²C device to operate on.</param>
        public SCD4x(I2cDevice device)
        {
            _device = device;
        }

        /// <inheritdoc/>
        public void Dispose() =>
            _device.Dispose();

        /// <summary>
        /// Calibrates the sensor to operate at a specific barometric pressure.
        /// Doing so will make measurements more accurate.
        /// </summary>
        /// <param name="pressure">The pressure to use when calibrating the sensor.</param>
        public void SetPressureCalibration(Pressure pressure)
        {
            Span<byte> buffer = stackalloc byte[5];

            _ = buffer[4];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, 0xE000);
            SHT4x.WriteUInt16AndCRC8(buffer[2..], (ushort)(pressure.Pascals * (1.0 / 100.0)));

            _device.Write(buffer);
            Thread.Sleep(1);
        }

        /// <summary>
        /// <para>
        /// Instructs the sensor to start performing periodic measurements.
        /// Measurements are available every 5 seconds.
        /// </para>
        /// 
        /// <para>
        /// <see cref="CheckDataReady"/> can be called to see if a measurement is available,
        /// and the measurement can then be read via <see cref="ReadPeriodicMeasurement"/>.
        /// </para>
        /// 
        /// <para>
        /// <see cref="StopPeriodicMeasurements"/> must be called to stop periodic measurements.
        /// </para>
        /// </summary>
        public void StartPeriodicMeasurements() =>
            _device.Write(StartPeriodicMeasurementBytes);

        /// <summary>
        /// <para>
        /// Checks if a periodic measurement is available.
        /// Once available, the measurement can be read via <see cref="ReadPeriodicMeasurement"/>.
        /// </para>
        /// 
        /// <para>
        /// <see cref="StartPeriodicMeasurements"/> must be called first.
        /// </para>
        /// </summary>
        /// <returns>If a measurement is available, <see langword="true"/>. Otherwise, <see langword="false"/>.</returns>
        public bool CheckDataReady()
        {
            _device.Write(CheckDataReadyStatusBytes);

            Thread.Sleep(1);

            Span<byte> buffer = stackalloc byte[3];
            _device.Read(buffer);

            ushort response = SHT4x.ReadUInt16AndCRC8(buffer[..3]);
            return (response & 0x7FF) != 0;
        }

        /// <summary>
        /// <para>
        /// Reads a periodic CO₂, humidity, and temperature measurement from the sensor.
        /// </para>
        /// 
        /// <para>
        /// <see cref="StartPeriodicMeasurements"/> must be called first.
        /// </para>
        /// </summary>
        /// <returns>A tuple of CO₂, humidity, and temperature.</returns>
        public (VolumeConcentration co2, RelativeHumidity humidity, Temperature temperature) ReadPeriodicMeasurement()
        {
            _device.Write(ReadPeriodicMeasurementBytes);

            Thread.Sleep(2);

            Span<byte> buffer = stackalloc byte[9];
            _device.Read(buffer);

            _ = buffer[8];
            ushort deviceCO2 = SHT4x.ReadUInt16AndCRC8(buffer[0..3]);
            ushort deviceTemperature = SHT4x.ReadUInt16AndCRC8(buffer[3..6]);
            ushort deviceHumidity = SHT4x.ReadUInt16AndCRC8(buffer[6..9]);

            VolumeConcentration co2 = VolumeConcentration.FromPartsPerMillion(deviceCO2);
            Temperature temp = Temperature.FromDegreesCelsius(Math.FusedMultiplyAdd(deviceTemperature, 35.0 / 13107.0, -45.0));
            RelativeHumidity humidity = RelativeHumidity.FromPercent(deviceHumidity * (100.0 / 65535.0));

            return (co2, humidity, temp);
        }

        /// <summary>
        /// Instructs the sensor to stop performing periodic measurements.
        /// </summary>
        public void StopPeriodicMeasurements()
        {
            _device.Write(StopPeriodicMeasurementBytes);
            Thread.Sleep(500);
        }
    }
}
