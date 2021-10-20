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
        };

        public static IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenSensor(I2cDevice device, IObservable<Measurement> dependencies) => new ObservableSps30(device, dependencies);

        protected override void DisposeCore() => _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            Sps30ParticulateData? particulateData = new Sps30ParticulateData();

            _sensor.ReadMeasurementsAsync((pd) =>
            {
                lock (_sensor)
                {
                    particulateData = pd;
                }
            }, cancellationToken);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                Sps30ParticulateData? localData;
                Console.WriteLine(_sensor.GetSerialNumber());
                Console.WriteLine(_sensor.GetArticleCode());
                lock (_sensor)
                {
                    localData = particulateData;
                }

                if(localData is not null)
                {
                    OnNextParticulate1_0PMassConcentrationMeasurement(localData.Value);
                    OnNextParticulate2_5PMassConcentrationMeasurement(localData.Value);
                    OnNextParticulate4_0PMassConcentrationMeasurement(localData.Value);
                    OnNextParticulate10_0PMassConcentrationMeasurement(localData.Value);

                    OnNextParticulate0_5PNumberConcentrationMeasurement(localData.Value);
                    OnNextParticulate1_0PNumberConcentrationMeasurement(localData.Value);
                    OnNextParticulate2_5PNumberConcentrationMeasurement(localData.Value);
                    OnNextParticulate4_0PNumberConcentrationMeasurement(localData.Value);
                    OnNextParticulate10_0PNumberConcentrationMeasurement(localData.Value);
                }
                    
            }
        }
    }
}
