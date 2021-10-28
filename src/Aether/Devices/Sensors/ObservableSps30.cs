using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aether.Devices.Drivers;
using Aether.Devices.Sensors.Metadata;

namespace Aether.Devices.Sensors
{
    internal class ObservableSps30 : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Sps30 _sensor;
        private readonly IObservable<Measurement> _dependencies;

        private ObservableSps30(I2cDevice device, IObservable<Measurement> dependencies)
        {
            _sensor = new Drivers.Sps30(device);
            _dependencies = dependencies;
            Start();
        }

        public static int DefaultAddress => Sps30.DefaultI2cAddress;

        public static string Manufacturer => "Sensirion";

        public static string Name => "SPS30";

        public static string Uri => "https://www.sensirion.com/en/environmental-sensors/particulate-matter-sensors-pm25/";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.MassConcentration),
            new MeasureInfo(Measure.NumberConcentration),
            new MeasureInfo(Measure.Length)
        };

        public static IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenSensor(I2cDevice device, IObservable<Measurement> dependencies) => new ObservableSps30(device, dependencies);

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
                            OnNextMassConcentration(particulateData.PM1_0.Value);

                        if (particulateData.PM2_5 is not null)
                            OnNextMassConcentration(particulateData.PM2_5.Value);

                        if (particulateData.PM4_0 is not null)
                            OnNextMassConcentration(particulateData.PM4_0.Value);

                        if (particulateData.PM10_0 is not null)
                            OnNextMassConcentration(particulateData.PM10_0.Value);

                        if (particulateData.P0_5 is not null)
                            OnNextNumberConcentration(particulateData.P0_5.Value);

                        if (particulateData.P1_0 is not null)
                            OnNextNumberConcentration(particulateData.P1_0.Value);

                        if (particulateData.P2_5 is not null)
                            OnNextNumberConcentration(particulateData.P2_5.Value);

                        if (particulateData.P4_0 is not null)
                            OnNextNumberConcentration(particulateData.P4_0.Value);

                        if (particulateData.P10_0 is not null)
                            OnNextNumberConcentration(particulateData.P10_0.Value);

                        if (particulateData.TypicalParticleSize is not null)
                            OnNextLength(particulateData.TypicalParticleSize.Value);
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
