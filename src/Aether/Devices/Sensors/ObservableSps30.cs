using System.Device.I2c;
using Aether.Devices.Drivers;
using Aether.Devices.Sensors.Metadata;

namespace Aether.Devices.Sensors
{
    internal class ObservableSps30 : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Sps30 _sensor;

        private ObservableSps30(I2cDevice device)
        {
            _sensor = new Sps30(device);
            Start();
        }

        public static int DefaultAddress => Sps30.DefaultI2cAddress;

        public static string Manufacturer => "Sensirion";

        public static string Name => "SPS30";

        public static string Uri => "https://www.sensirion.com/en/environmental-sensors/particulate-matter-sensors-pm25/";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.PM1_0),
            new MeasureInfo(Measure.PM2_5),
            new MeasureInfo(Measure.PM4_0),
            new MeasureInfo(Measure.PM10_0),
            new MeasureInfo(Measure.P0_5),
            new MeasureInfo(Measure.P1_0),
            new MeasureInfo(Measure.P2_5),
            new MeasureInfo(Measure.P4_0),
            new MeasureInfo(Measure.P10_0),
            new MeasureInfo(Measure.TypicalParticleSize)
        };

        public static IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenSensor(I2cDevice device, IObservable<Measurement> dependencies) => new ObservableSps30(device);

        protected override void DisposeCore() => _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            _sensor.StartMeasurement();

            try
            {
                while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                {
                    bool? sensorDataReady = _sensor.CheckSensorDataReady();

                    if(sensorDataReady is not null && sensorDataReady.Value)
                    {
                        Sps30ParticulateData particulateData = _sensor.ReadMeasuredValues();

                        if (particulateData.PM1_0 is not null)
                            OnNextPM1_0(particulateData.PM1_0.Value);

                        if (particulateData.PM2_5 is not null)
                            OnNextPM2_5(particulateData.PM2_5.Value);

                        if (particulateData.PM4_0 is not null)
                            OnNextPM4_0(particulateData.PM4_0.Value);

                        if (particulateData.PM10_0 is not null)
                            OnNextPM10_0(particulateData.PM10_0.Value);

                        if (particulateData.P0_5 is not null)
                            OnNextP0_5(particulateData.P0_5.Value);

                        if (particulateData.P1_0 is not null)
                            OnNextP1_0(particulateData.P1_0.Value);

                        if (particulateData.P2_5 is not null)
                            OnNextP2_5(particulateData.P2_5.Value);

                        if (particulateData.P4_0 is not null)
                            OnNextP4_0(particulateData.P4_0.Value);

                        if (particulateData.P10_0 is not null)
                            OnNextP10_0(particulateData.P10_0.Value);

                        if (particulateData.TypicalParticleSize is not null)
                            OnNextTypicalParticleSize(particulateData.TypicalParticleSize.Value);
                    }
                    
                }
            }
            finally
            {
                _sensor.StopMeasurement();
            }
        }
    }
}
