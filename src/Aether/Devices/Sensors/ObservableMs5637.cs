using System.Device.I2c;
using System.Reactive.Linq;
using Aether.Devices.Sensors.Metadata;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    internal sealed class ObservableMs5637 : I2cSensorFactory
    {
        public static ObservableMs5637 Instance { get; } = new ObservableMs5637();

        public override int DefaultAddress => Drivers.Ms5637.DefaultI2cAddress;

        public override string Manufacturer => "TE Connectivity";

        public override string Name => "MS5637";

        public override string Uri => "https://www.te.com/commerce/DocumentDelivery/DDEController?Action=srchrtrv&DocNm=MS5637-02BA03&DocType=Data+Sheet&DocLang=English";

        public override bool CanSimulate => true;

        public override IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.BarometricPressure),
            new MeasureInfo(Measure.Temperature)
        };

        public override IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public override IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public override IObservable<Measurement> OpenSensor(Func<I2cDevice> deviceFunc, IObservable<Measurement> dependencies) =>
            Observable.Create(async (IObserver<Measurement> measurements, CancellationToken cancellationToken) =>
            {
                using I2cDevice device = deviceFunc();
                using var sensor = new Drivers.Ms5637(device);
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
                using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

                while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                {
                    (Temperature temperature, Pressure pressure) =
                        sensor.ReadTemperatureAndPressure();

                    measurements.OnNext(Measurement.FromTemperature(temperature));
                    measurements.OnNext(Measurement.FromPressure(pressure));
                }
            });

        protected override I2cDevice CreateSimulatedI2cDevice() =>
            new Simulated.SimulatedMS5637();
    }
}
