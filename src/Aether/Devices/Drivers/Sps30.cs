using System.Device.I2c;
using System.Diagnostics;
using System.Text;
using Aether.CustomUnits;
using UnitsNet;

namespace Aether.Devices.Drivers
{

    public record struct Sps30ParticulateData(
        MassConcentration? PM1_0,
        MassConcentration? PM2_5,
        MassConcentration? PM4_0,
        MassConcentration? PM10_0,
        NumberConcentration? P0_5,
        NumberConcentration? P1_0,
        NumberConcentration? P2_5,
        NumberConcentration? P4_0,
        NumberConcentration? P10_0,
        Length? TypicalParticleSize);

    /// <summary>
    /// Air Particulate Sensor SPS30
    /// </summary>
    public sealed class Sps30 : System.IDisposable
    {        
        /// <summary>
        /// The default I²C address of this device.
        /// </summary>
        public const int DefaultI2cAddress = 0x69;

        private readonly I2cDevice _device;

        /// <summary>
        /// Instantiates a new <see cref="Sps30"/>.
        /// </summary>
        /// <param name="device">The I²C device to operate on.</param>
        public Sps30(I2cDevice device)
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
            ReadOnlySpan<byte> resetDeviceCommand = new byte[] { 0xD3, 0x04 };

            _device.Write(resetDeviceCommand);

            Thread.Sleep(1);
        }

        /// <summary>
        /// Manually starts a fan cleaning cycle.
        /// </summary>
        public void StartFanCleaning()
        {
            ReadOnlySpan<byte> startFanCleaningCommand = new byte[] { 0x56, 0x07 };

            _device.Write(startFanCleaningCommand);

            Thread.Sleep(1);
        }

        /// <summary>
        /// Sets the fan cleaning interval.
        /// </summary>
        /// <remarks>If ther provided interval is 0. The automatic cleaning interval will be disabled.</remarks>
        /// <param name="seconds">The interval in seconds which the cleaning cycle will be ran.</param>
        public void SetFanCleaningInterval(int seconds)
        {
            if(seconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, $"{nameof(seconds)} must be zero or greater.");
            }

            Span<byte> writeCleaningIntervalCommand = stackalloc byte[8];

            // Cleaning interval command
            writeCleaningIntervalCommand[0] = 0x80;
            writeCleaningIntervalCommand[1] = 0x04;

            ReadOnlySpan<byte> interval = BitConverter.GetBytes(seconds);

            Sensirion.WriteUInt16BigEndianAndCRC8(writeCleaningIntervalCommand.Slice(2, 3), (ushort)(seconds >> 16)); // High Bits
            Sensirion.WriteUInt16BigEndianAndCRC8(writeCleaningIntervalCommand.Slice(5, 3), (ushort)seconds); // Low Bits

            // Write Set Cleaning Interval Command
            _device.Write(writeCleaningIntervalCommand);

            Thread.Sleep(1);
        }

        /// <summary>
        /// Gets the current fan cleaning interval. 
        /// </summary>
        /// <remarks>If the value has been previously set without having reset the device, the previous value will be read.</remarks>
        /// <returns>The current fan cleaning interval if the device has been reset after setting the interval.</returns>
        public int? GetFanCleaningInterval()
        {
            ReadOnlySpan<byte> readCleaningIntervalCommand = new byte[] { 0x80, 0x04 };
            Span<byte> receivedCleaningInterval = stackalloc byte[6];

            // Write Read Cleaning Interval Command
            _device.Write(readCleaningIntervalCommand);

            _device.Read(receivedCleaningInterval);

            return (int?)Sensirion.ReadUInt32BigEndianAndCRC8(receivedCleaningInterval);
        }

        /// <summary>
        /// Gets the sensors information. The type of information will vary based on the value of <paramref name="getDeviceInformationCommand"/>
        /// </summary>
        /// <param name="getDeviceInformationCommand">The get device information command bytes.</param>
        /// <returns>An ASCII string representing the requested device data.</returns>
        private string? GetDeviceInformation(ReadOnlySpan<byte> getDeviceInformationCommand)
        {
            Span<byte> deviceInformationWithCRC = stackalloc byte[48];

            // Write get device information command and read information
            _device.Write(getDeviceInformationCommand);

            _device.Read(deviceInformationWithCRC);

            Span<byte> deviceInformationBytes = stackalloc byte[32];

            for (int infoCrcIndex = 0, devInfoIndex = 0; infoCrcIndex + 3 < deviceInformationWithCRC.Length; infoCrcIndex+=3)
            {
                ushort? stringChars = Sensirion.ReadUInt16BigEndianAndCRC8(deviceInformationWithCRC.Slice(infoCrcIndex, 3));
                
                if(stringChars is null)
                {
                    return null;
                }

                deviceInformationBytes[devInfoIndex++] = (byte)(stringChars >> 8);
                deviceInformationBytes[devInfoIndex++] = (byte)stringChars;
            }

            return Encoding.ASCII.GetString(deviceInformationBytes);
        }

        /// <summary>
        /// Gets the devices serial number.
        /// </summary>
        /// <returns>The serial number as an ASCII string. If CRC failed, <see langword="null"/>.</returns>
        public string? GetSerialNumber()
        {
            ReadOnlySpan<byte> getArticleCodeCommand = new byte[] { 0xD0, 0x33 };
            
            return GetDeviceInformation(getArticleCodeCommand);
        }

        /// <summary>
        /// Gets the devices article code.
        /// </summary>
        /// <returns>The article code as an ASCII string. If CRC failed, <see langword="null"/>.</returns>
        public string? GetArticleCode() => GetDeviceInformation(new byte[] { 0xD0, 0x25 });

        /// <summary>
        /// Reads sensor measurement values.
        /// </summary>
        /// <returns>The measurement data. If a CRC error occurred, <see langword="null"/>.</returns>
        public Sps30ParticulateData ReadMeasuredValues()
        {
            ReadOnlySpan<byte> readMeasuredValuesCommand = new byte[] { 0x03, 0x00 };

            Span<byte> measuredValuesWithCRC = stackalloc byte[60];

            // Write read measured values command, then read measured values with CRC data
            _device.Write(readMeasuredValuesCommand);

            _device.Read(measuredValuesWithCRC);

            // Parse values and populate particulate data
            // Values are IEEE754 float values 4 bytes each

            // Parse Mass Concentration PM1.0
            MassConcentration? PM1_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(0, 6)) is float mPM1_0 ? new MassConcentration(mPM1_0, UnitsNet.Units.MassConcentrationUnit.MicrogramPerCubicMeter) : null;

            // Parse Mass Concentration PM2.5
            MassConcentration? PM2_5 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(6, 6)) is float mPM2_5 ? new MassConcentration(mPM2_5, UnitsNet.Units.MassConcentrationUnit.MicrogramPerCubicMeter) : null;

            // Parse Mass Concentration PM4.0
            MassConcentration? PM4_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(12, 6)) is float mPM4_0 ? new MassConcentration(mPM4_0, UnitsNet.Units.MassConcentrationUnit.MicrogramPerCubicMeter) : null;

            // Parse Mass Concentration PM10
            MassConcentration? PM10_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(18, 6)) is float mPM10_0 ? new MassConcentration(mPM10_0, UnitsNet.Units.MassConcentrationUnit.MicrogramPerCubicMeter) : null;

            // Parse Number Concentration PM0_5
            NumberConcentration? P0_5 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(24, 6)) is float mP0_5 ? new NumberConcentration(mP0_5) : null;

            // Parse Number Concentration PM1_0
            NumberConcentration? P1_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(30, 6)) is float mP1_0 ? new NumberConcentration(mP1_0) : null;

            // Parse Number Concentration PM2_5
            NumberConcentration? P2_5 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(36, 6)) is float mP2_5 ? new NumberConcentration(mP2_5) : null;

            // Parse Number Concentration PM4_0
            NumberConcentration? P4_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(42, 6)) is float mP4_0 ? new NumberConcentration(mP4_0) : null;

            // Parse Number Concentration PM10
            NumberConcentration? P10_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(48, 6)) is float mP10_0 ? new NumberConcentration(mP10_0) : null;

            // Parse Typical Particle Size (Length as micrometer)
            Length? typicalParticalSize = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(54, 6)) is float mTypicalParticleSize ? new Length(mTypicalParticleSize, UnitsNet.Units.LengthUnit.Micrometer) : null;

            return new Sps30ParticulateData(
                PM1_0,
                PM2_5,
                PM4_0,
                PM10_0,
                P0_5,
                P1_0,
                P2_5,
                P4_0,
                P10_0,
                typicalParticalSize);
        }

        /// <summary>
        /// Process measurement data into a float value.
        /// </summary>
        /// <param name="measurementData">The measurement data bytes.</param>
        /// <returns>A float value of the measurement data. If a CRC error occurred, <see langword="null"/>.</returns>
        private float? ProcessMeasurementBytes(ReadOnlySpan<byte> measurementData) =>
            Sensirion.ReadUInt32BigEndianAndCRC8(measurementData) is uint32 x ? BitConverter.Int32BitsToSingle((int)x) : null;

        /// <summary>
        /// Starts measurements on the device.
        /// </summary>
        public void StartMeasurement()
        {
            // Start measurement command 0x00, 0x01
            // Measurement mode 0x03 with dummy byte 0x00 and CRC
            Span<byte> bytes = stackalloc byte[3];
            Sensirion.WriteUInt16BigEndianAndCRC8(bytes, 0x0300);

            // Start measurement command 0x00, 0x01
            // Measurement mode 0x03 with dummy byte 0x00
            ReadOnlySpan<byte> startMeasurementCommand = stackalloc byte[5] { 0x00, 0x10, bytes[0], bytes[1], bytes[2] };

            //ReadOnlySpan<byte> startMeasurementCommand = stackalloc byte[5] { 0x00, 0x10, 0x03, 0x00, 0xAC };

            // Write start measurement
            _device.Write(startMeasurementCommand);

            Thread.Sleep(1);
        }

        /// <summary>
        /// Stops measurements on the device.
        /// </summary>
        public void StopMeasurement()
        {
            ReadOnlySpan<byte> stopMeasurementCommand = new byte[] { 0x01, 0x04 };

            // Write stop measurement command
            _device.Write(stopMeasurementCommand);

            Thread.Sleep(1);
        }

        /// <summary>
        /// Checks if the sensor data is ready to be read.
        /// </summary>
        /// <returns>True if ready, false if not. If a CRC error occurred, <see langword="null"./></returns>
        public bool? CheckSensorDataReady()
        {
            ReadOnlySpan<byte> readDataReadyCommand = new byte[] { 0x02, 0x02 };
            Span<byte> dataReadyResult = stackalloc byte[3];

            // Write check sensor data ready command
            _device.Write(readDataReadyCommand);

            // Read data ready result
            _device.Read(dataReadyResult);

            return Sensirion.ReadUInt16BigEndianAndCRC8(dataReadyResult) == 1;
        }
    }
}
