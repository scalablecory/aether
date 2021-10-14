using Aether.CustomUnits;
using Aether.Devices.Sensors.Metadata;
using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal class ObservableSgp4x : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Drivers.Sgp4x _sensor;

        private ObservableSgp4x(I2cDevice device)
        {
            _sensor = new Drivers.Sgp4x(device);
            Start();
        }

        public static int DefaultAddress => Drivers.Sgp4x.DefaultI2cAddress;

        public static string Manufacturer => "Sensirion";

        public static string Name => "SGP4x";

        public static string Uri => "https://www.sensirion.com/en/environmental-sensors/gas-sensors/sgp40/";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.VOC)
        };

        public static IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;

        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenSensor(I2cDevice device, IObservable<Measurement> dependencies) => new ObservableSgp4x(device);

        protected override void DisposeCore() => _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                VolatileOrganicCompoundIndex? vocIndex = _sensor.GetVOCRawMeasure();

                if (vocIndex is not null) OnNextVolitileOrganicCompound(vocIndex.Value);
            }
        }
    }
}
