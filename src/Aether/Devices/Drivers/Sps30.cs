using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aether.Devices.Drivers
{

    public struct Sps30ParticulateData
    {
        public float MassConcentrationPM1_0;
        public float MassConcentrationPM2_5;
        public float MassConcentrationPM4_0;
        public float MassConcentrationPM10_0;
        public float NumberConcentrationPM0_5;
        public float NumberConcentrationPM1_0;
        public float NumberConcentrationPM2_5;
        public float NumberConcentrationPM4_0;
        public float NumberConcentrationPM10_0;
        public float TypicalParticalSize;
    }

    /// <summary>
    /// Air Particulate Sensor SPS30
    /// </summary>
    public sealed class Sps30 : System.IDisposable
    {

        /// <summary>
        /// The default I²C address of this device.
        /// </summary>
        public const int DefaultI2cAddress = 0x69;

        private readonly int MAX_CONSECUTIVE_READ_FAILURES = 30;

        private readonly I2cDevice _device;

        private bool _isReadingSensorData = false;

        /// <summary>
        /// Instantiates a new <see cref="Sgp4x"/>.
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
            bool sensorBeingRead = false;

            lock (_device)
            {
                sensorBeingRead = _isReadingSensorData;
            }

            if (sensorBeingRead)
            {
                throw new InvalidOperationException("Device cannot be reset while sensor data is being read");
            }

            ReadOnlySpan<byte> resetDeviceCommand = stackalloc byte[] { 0xD3, 0x04 };

            _device.Write(resetDeviceCommand);

            Thread.Sleep(1);
        }

        /// <summary>
        /// Manually starts a fan cleaning cycle.
        /// </summary>
        public void StartFanCleaning()
        {
            ReadOnlySpan<byte> startFanCleaningCommand = stackalloc byte[] { 0x56, 0x07 };

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
            Debug.Assert(seconds >= 0);

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

            ReadOnlySpan<byte> readCleaningIntervalCommand = stackalloc byte[] { 0x80, 0x04 };
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
            // ReadOnlySpan<byte> getDeviceDataCommand = stackalloc byte[2] 
            Span<byte> deviceInformationWithCRC = stackalloc byte[48];

            // Write get device information command
            _device.Write(getDeviceInformationCommand);

            // Read raw device data
            _device.Read(deviceInformationWithCRC);

            byte[] deviceInformationBytes = new byte[32];

            for (int i = 0; i + 3 < deviceInformationWithCRC.Length; i+=4)
            {
                ushort? stringChars = Sensirion.ReadUInt16BigEndianAndCRC8(deviceInformationWithCRC.Slice(i, 3));
                
                if(stringChars is null)
                {
                    return null;
                }

                deviceInformationBytes[i] = (byte)stringChars;
                deviceInformationBytes[i + 1] = (byte)(stringChars >> 8);
            }

            return Encoding.ASCII.GetString(deviceInformationBytes);
        }

        /// <summary>
        /// Gets the devices serial number.
        /// </summary>
        /// <returns>The serial number as an ASCII string. If CRC failed, <see langword="null"/>.</returns>
        public string? GetSerialNumber()
        {
            ReadOnlySpan<byte> getArticleCodeCommand = stackalloc byte[] { 0xD0, 0x33 };
            
            return GetDeviceInformation(getArticleCodeCommand);
        }

        /// <summary>
        /// Gets the devices article code.
        /// </summary>
        /// <returns>The article code as an ASCII string. If CRC failed, <see langword="null"/>.</returns>
        public string? GetArticleCode()
        {
            ReadOnlySpan<byte> getArticleCodeCommand = stackalloc byte[] { 0xD0, 0x25 };

            return GetDeviceInformation(getArticleCodeCommand);
        }

        /// <summary>
        /// Reads measurements from the sensor.
        /// </summary>
        /// <param name="callback">The callback that is invoked once new sensor data is ready.</param>
        /// <param name="cancellationToken">The cancelation token.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="callback"/> is <see langword="null"/>.</exception>
        public void ReadMeasurementsAsync(Action<Sps30ParticulateData?> callback, CancellationToken cancellationToken)
        {
            if(callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            lock (_device)
            {
                if (!_isReadingSensorData)
                {
                    _isReadingSensorData = true;

                    StartMeasurement();

                    Task.Run(() =>
                    {
                        int consecutiveSensorReadFailures = 0;
                        Action<Sps30ParticulateData?> localCallback = callback;

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            // Check if the sensor data is ready
                            bool? dataReady = CheckSensorDataReady();

                            if (dataReady is null)
                            {
                                // If we have had consecutive read failures, abort.
                                if (++consecutiveSensorReadFailures >= MAX_CONSECUTIVE_READ_FAILURES)
                                    break;

                                // This is due to CRC error
                                Thread.Sleep(1000);
                            }
                            else if(!dataReady.Value)
                            {
                                Thread.Sleep(1000);
                            }
                            else if (dataReady.Value)
                            {
                                // Sensor data ready, read it
                                Sps30ParticulateData? sensorData = ReadMeasuredValues();

                                if (sensorData is null)
                                {
                                    // If we have had consecutive read failures, abort.
                                    if (++consecutiveSensorReadFailures >= MAX_CONSECUTIVE_READ_FAILURES)
                                        break;
                                }
                                else
                                {
                                    consecutiveSensorReadFailures = 0;

                                    try
                                    {
                                        localCallback(sensorData);
                                    }
                                    catch (Exception)
                                    {
                                        // If callback throws, abort.
                                        break;
                                    }
                                }
                            }
                        }

                        StopMeasurement();

                        lock (_device)
                        {
                            _isReadingSensorData = false;
                        }

                    }, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Reads sensor measurement values.
        /// </summary>
        /// <returns>The measurement data. If a CRC error occurred, <see langword="null"/>.</returns>
        private Sps30ParticulateData? ReadMeasuredValues()
        {
            Sps30ParticulateData particulateData = new Sps30ParticulateData();

            ReadOnlySpan<byte> readMeasuredValuesCommand = stackalloc byte[] { 0x03, 0x00 };

            Span<byte> measuredValuesWithCRC = stackalloc byte[60];

            // Write read measured values command
            _device.Write(readMeasuredValuesCommand);

            // Read measured values with CRC data
            _device.Read(measuredValuesWithCRC);

            // Parse values and populate particulate data
            // Values are IEEE754 float values 4 bytes each

            float? measurement = null;

            // Parse Mass Concentration PM1.0
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(0, 6));

            if (measurement is null)
                return null;

            particulateData.MassConcentrationPM1_0 = measurement.Value;

            // Parse Mass Concentration PM2.5
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(6, 6));

            if (measurement is null)
                return null;

            particulateData.MassConcentrationPM2_5 = measurement.Value;

            // Parse Mass Concentration PM4.0
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(12, 6));

            if (measurement is null)
                return null;

            particulateData.MassConcentrationPM4_0 = measurement.Value;

            // Parse Mass Concentration PM10
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(18, 6));

            if (measurement is null)
                return null;

            particulateData.MassConcentrationPM10_0 = measurement.Value;

            // Parse Number Concentration PM0_5
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(24, 6));

            if (measurement is null)
                return null;

            particulateData.NumberConcentrationPM0_5 = measurement.Value;

            // Parse Number Concentration PM1_0
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(30, 6));

            if (measurement is null)
                return null;

            particulateData.NumberConcentrationPM1_0 = measurement.Value;

            // Parse Number Concentration PM2_5
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(36, 6));

            if (measurement is null)
                return null;

            particulateData.NumberConcentrationPM2_5 = measurement.Value;

            // Parse Number Concentration PM4_0
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(42, 6));

            if (measurement is null)
                return null;

            particulateData.NumberConcentrationPM4_0 = measurement.Value;

            // Parse Number Concentration PM10
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(48, 6));

            if (measurement is null)
                return null;

            particulateData.NumberConcentrationPM10_0 = measurement.Value;

            // Parse Typical Particle Size
            measurement = ProcessMeasurementBytes(measuredValuesWithCRC.Slice(54, 6));

            if (measurement is null)
                return null;

            particulateData.TypicalParticalSize = measurement.Value;

            return particulateData;
        }

        /// <summary>
        /// Process measurement data into a float value.
        /// </summary>
        /// <param name="measurementData">The measurement data bytes.</param>
        /// <returns>A float value of the measurement data. If a CRC error occurred, <see langword="null"/>.</returns>
        private float? ProcessMeasurementBytes(ReadOnlySpan<byte> measurementData)
        {
            Debug.Assert(measurementData.Length > 6);

            ushort? upperBytes = Sensirion.ReadUInt16BigEndianAndCRC8(measurementData.Slice(0, 3));
            ushort? lowerBytes = Sensirion.ReadUInt16BigEndianAndCRC8(measurementData.Slice(3, 3));

            if (upperBytes is null || lowerBytes is null)
            {
                return null;
            }

            ReadOnlySpan<byte> measurementBytes = stackalloc byte[4]
            {   (byte)upperBytes,
                (byte)(upperBytes >> 8),
                (byte)lowerBytes,
                (byte)(lowerBytes >> 8)
            };

            return BitConverter.ToSingle(measurementBytes);
        }

        /// <summary>
        /// Starts measurements on the device.
        /// </summary>
        private void StartMeasurement()
        {
            // Start measurement command 0x00, 0x01
            // Measurement mode 0x03 with dummy byte 0x00
            ReadOnlySpan<byte> startMeasurementCommand = stackalloc byte[4] { 0x00, 0x01, 0x03, 0x00 };

            // Write start measurement
            _device.Write(startMeasurementCommand);

            Thread.Sleep(1);
        }

        /// <summary>
        /// Stops measurements on the device.
        /// </summary>
        private void StopMeasurement()
        {
            ReadOnlySpan<byte> stopMeasurementCommand = stackalloc byte[] { 0x01, 0x04 };

            // Write stop measurement command
            _device.Write(stopMeasurementCommand);

            Thread.Sleep(1);
        }

        /// <summary>
        /// Checks if the sensor data is ready to be read.
        /// </summary>
        /// <returns>True if ready, false if not. If a CRC error occurred, <see langword="null"./></returns>
        private bool? CheckSensorDataReady()
        {
            ReadOnlySpan<byte> readDataReadyCommand = stackalloc byte[] { 0x02, 0x02 };
            Span<byte> dataReadyResult = stackalloc byte[2];

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
            return (resultData >> 8) == 1;
        }
    }
}
