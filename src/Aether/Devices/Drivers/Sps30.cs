using System.Device.I2c;
using System.Diagnostics;
using System.Text;
using Aether.CustomUnits;
using UnitsNet;

namespace Aether.Devices.Drivers
{

    public record struct Sps30ParticulateData(
        MassConcentration PM1_0,
        MassConcentration PM2_5,
        MassConcentration PM4_0,
        MassConcentration PM10_0,
        NumberConcentration P0_5,
        NumberConcentration P1_0,
        NumberConcentration P2_5,
        NumberConcentration P4_0,
        NumberConcentration P10_0,
        Length TypicalParticleSize);

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

        private bool _isReadingSensorData = false;

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
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, $"{nameof(seconds)} must be zero or greater.");

            Span<byte> writeCleaningIntervalCommand = stackalloc byte[8];

            // Cleaning interval command
            writeCleaningIntervalCommand[0] = 0x80;
            writeCleaningIntervalCommand[1] = 0x04;

            ReadOnlySpan<byte> interval = BitConverter.GetBytes(seconds);

            Sensirion.WriteUInt16BigEndianAndCRC8(writeCleaningIntervalCommand.Slice(2, 3), BitConverter.ToUInt16(interval.Slice(0, 2)));
            Sensirion.WriteUInt16BigEndianAndCRC8(writeCleaningIntervalCommand.Slice(5, 3), BitConverter.ToUInt16(interval.Slice(2, 2)));

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

            ushort? msb = Sensirion.ReadUInt16BigEndianAndCRC8(receivedCleaningInterval.Slice(0, 3));
            ushort? lsb = Sensirion.ReadUInt16BigEndianAndCRC8(receivedCleaningInterval.Slice(3, 3));

            if (msb == null || lsb == null)
                return null;


            ReadOnlySpan<byte> intervalBytes = stackalloc byte[] {
                (byte)msb.Value,
                (byte)(msb.Value >> 8),
                (byte)lsb.Value,
                (byte)(lsb.Value >> 8)
            };

            return BitConverter.ToInt32(intervalBytes);
        }

        /// <summary>
        /// Gets the sensors information. The type of information will vary based on the value of <paramref name="getDeviceInformationCommand"/>
        /// </summary>
        /// <param name="getDeviceInformationCommand">The get device information command bytes.</param>
        /// <returns>An ASCII string representing the requested device data.</returns>
        private string? GetDeviceInformation(ReadOnlySpan<byte> getDeviceInformationCommand)
        {
            Span<byte> deviceInformationWithCRC = stackalloc byte[48];

            // Write get device information command
            _device.Write(getDeviceInformationCommand);
            Thread.Sleep(1);

            // Read raw device data
            _device.Read(deviceInformationWithCRC);

            byte[] deviceInformationBytes = new byte[32];

            for (int infoCrcIndex = 0, devInfoIndex = 0; infoCrcIndex + 3 < deviceInformationWithCRC.Length; infoCrcIndex+=3)
            {
                ushort? stringChars = Sensirion.ReadUInt16BigEndianAndCRC8(deviceInformationWithCRC.Slice(infoCrcIndex, 3));
                
                if(stringChars is null)
                {
                    return null;
                }

                deviceInformationBytes[devInfoIndex++] = (byte)stringChars;
                deviceInformationBytes[devInfoIndex++] = (byte)(stringChars >> 8);
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
        public string? GetArticleCode()
        {
            ReadOnlySpan<byte> getArticleCodeCommand = new byte[] { 0xD0, 0x25 };

            return GetDeviceInformation(getArticleCodeCommand);
        }

        /// <summary>
        /// Reads sensor measurement values.
        /// </summary>
        /// <returns>The measurement data. If a CRC error occurred, <see langword="null"/>.</returns>
        public Sps30ParticulateData? ReadMeasuredValues()
        {
            ReadOnlySpan<byte> readMeasuredValuesCommand = new byte[] { 0x03, 0x00 };

            Span<byte> measuredValuesWithCRC = stackalloc byte[60];

            // Write read measured values command
            _device.Write(readMeasuredValuesCommand);

            // Read measured values with CRC data
            _device.Read(measuredValuesWithCRC);

            // Parse values and populate particulate data
            // Values are IEEE754 float values 4 bytes each

            float? mPM1_0, mPM2_5, mPM4_0, mPM10_0, mP0_5, mP1_0, mP2_5, mP4_0, mP10_0, mTypicalParticleSize;

            // Parse Mass Concentration PM1.0
            mPM1_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(0, 6));

            if (mPM1_0 is null)
                return null;

            // Parse Mass Concentration PM2.5
            mPM2_5 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(6, 6));

            if (mPM2_5 is null)
                return null;

            // Parse Mass Concentration PM4.0
            mPM4_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(12, 6));

            if (mPM4_0 is null)
                return null;

            // Parse Mass Concentration PM10
            mPM10_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(18, 6));

            if (mPM10_0 is null)
                return null;

            // Parse Number Concentration PM0_5
            mP0_5 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(24, 6));

            if (mP0_5 is null)
                return null;

            // Parse Number Concentration PM1_0
            mP1_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(30, 6));

            if (mP1_0 is null)
                return null;

            // Parse Number Concentration PM2_5
            mP2_5 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(36, 6));

            if (mP2_5 is null)
                return null;

            // Parse Number Concentration PM4_0
            mP4_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(42, 6));

            if (mP4_0 is null)
                return null;

            // Parse Number Concentration PM10
            mP10_0 = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(48, 6));

            if (mP10_0 is null)
                return null;

            // Parse Typical Particle Size (Length as micrometer)
            mTypicalParticleSize = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(54, 6));

            if (mTypicalParticleSize is null)
                return null;

            return new Sps30ParticulateData(
                new MassConcentration(mPM1_0.Value, UnitsNet.Units.MassConcentrationUnit.MicrogramPerCubicMeter),
                new MassConcentration(mPM2_5.Value, UnitsNet.Units.MassConcentrationUnit.MicrogramPerCubicMeter),
                new MassConcentration(mPM4_0.Value, UnitsNet.Units.MassConcentrationUnit.MicrogramPerCubicMeter),
                new MassConcentration(mPM10_0.Value, UnitsNet.Units.MassConcentrationUnit.MicrogramPerCubicMeter),
                new NumberConcentration(mP0_5.Value),
                new NumberConcentration(mP1_0.Value),
                new NumberConcentration(mP2_5.Value),
                new NumberConcentration(mP4_0.Value),
                new NumberConcentration(mP10_0.Value),
                new Length(mTypicalParticleSize.Value, UnitsNet.Units.LengthUnit.Micrometer));
        }

        /// <summary>
        /// Process measurement data into a float value.
        /// </summary>
        /// <param name="measurementData">The measurement data bytes.</param>
        /// <returns>A float value of the measurement data. If a CRC error occurred, <see langword="null"/>.</returns>
        private float? ProcessMeasurementBytes(ReadOnlySpan<byte> measurementData)
        {
            if(measurementData.Length != 6)
            {
                throw new ArgumentException(nameof(measurementData), "Measurement data must be exactly 6 bytes in size");
            }

            ushort? upperBytes = Sensirion.ReadUInt16BigEndianAndCRC8(measurementData.Slice(0, 3));
            ushort? lowerBytes = Sensirion.ReadUInt16BigEndianAndCRC8(measurementData.Slice(3, 3));

            if (upperBytes is null || lowerBytes is null)
            {
                return null;
            }

            // Data received is BigEndian, handle the byte filtering and reording here
            ReadOnlySpan<byte> measurementBytes = stackalloc byte[4]
            {
                (byte)lowerBytes,
                (byte)(lowerBytes >> 8),
                (byte)upperBytes,
                (byte)(upperBytes >> 8),
            };

            return BitConverter.ToSingle(measurementBytes);
        }

        /// <summary>
        /// Starts measurements on the device.
        /// </summary>
        public void StartMeasurement()
        {
            Span<byte> bytes = stackalloc byte[3];
            Sensirion.WriteUInt16BigEndianAndCRC8(bytes, 0x0300);

            // Start measurement command 0x00, 0x01
            // Measurement mode 0x03 with dummy byte 0x00
            ReadOnlySpan<byte> startMeasurementCommand = stackalloc byte[5] { 0x00, 0x10, bytes[0], bytes[1], bytes[2] };

            lock(_device)
            {
                if (_isReadingSensorData)
                {
                    throw new InvalidOperationException("Sensor has already started measurements");
                }

                // Write start measurement
                _device.Write(startMeasurementCommand);

                _isReadingSensorData = true;
            }

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

            _isReadingSensorData = false;

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

            ushort? resultData = Sensirion.ReadUInt16BigEndianAndCRC8(dataReadyResult);

            if(resultData is null)
            {
                return null;
            }

            // Hi byte is always 0x00 so ignore. Low byte will be 0x01 for ready, 0x00 for not ready.
            return resultData == 1;
        }
    }
}
