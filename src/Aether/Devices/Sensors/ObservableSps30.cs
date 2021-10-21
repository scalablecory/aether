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
            new MeasureInfo(Measure.Particulate1_0PMassConcentration),
            new MeasureInfo(Measure.Particulate2_5PMassConcentration),
            new MeasureInfo(Measure.Particulate4_0PMassConcentration),
            new MeasureInfo(Measure.Particulate10_0PMassConcentration),
            new MeasureInfo(Measure.Particulate0_5NumberConcentration),
            new MeasureInfo(Measure.Particulate1_0NumberConcentration),
            new MeasureInfo(Measure.Particulate2_5NumberConcentration),
            new MeasureInfo(Measure.Particulate4_0NumberConcentration),
            new MeasureInfo(Measure.Particulate10_0NumberConcentration),
            new MeasureInfo(Measure.ParticulateTypicalSize)
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
                        Sps30ParticulateData? particulateData = _sensor.ReadMeasuredValues();

                        if (particulateData is not null)
                        {
                            OnNextParticulate1_0PMassConcentrationMeasurement(particulateData.Value);
                            OnNextParticulate2_5PMassConcentrationMeasurement(particulateData.Value);
                            OnNextParticulate4_0PMassConcentrationMeasurement(particulateData.Value);
                            OnNextParticulate10_0PMassConcentrationMeasurement(particulateData.Value);

                            OnNextParticulate0_5PNumberConcentrationMeasurement(particulateData.Value);
                            OnNextParticulate1_0PNumberConcentrationMeasurement(particulateData.Value);
                            OnNextParticulate2_5PNumberConcentrationMeasurement(particulateData.Value);
                            OnNextParticulate4_0PNumberConcentrationMeasurement(particulateData.Value);
                            OnNextParticulate10_0PNumberConcentrationMeasurement(particulateData.Value);
                            OnNextParticulateTypicalSize(particulateData.Value);
                        }
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
