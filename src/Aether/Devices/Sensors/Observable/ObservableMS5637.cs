using Aether.Devices.Sensors.Metadata;
using System.Device.I2c;
using UnitsNet;

namespace Aether.Devices.Sensors.Observable
{
    internal sealed class ObservableMS5637 : ObservableSensor, IObservableI2CSensorFactory
    {
        private Drivers.MS5637 _sensor;

        private ObservableMS5637(I2cDevice device)
        {
            _sensor = new Drivers.MS5637(device);
        }

        protected override void DisposeCore() =>
            _sensor.Dispose();

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5_000));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                (Temperature temperature, Pressure pressure) =
                    _sensor.ReadTemperatureAndPressure();

                OnNextTemperature(temperature);
                OnNextBarometricPressure(pressure);
            }
        }

        #region IObservableI2CSensorFactory

        public static int DefaultAddress => 0x76;

        public static string Manufacturer => "TE Connectivity";

        public static string Name => "MS5637";

        public static string Uri => "https://www.te.com/commerce/DocumentDelivery/DDEController?Action=srchrtrv&DocNm=MS5637-02BA03&DocType=Data+Sheet&DocLang=English";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.Pressure),
            new MeasureInfo(Measure.Temperature)
        };

        public static IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenDevice(I2cDevice device, IEnumerable<ObservableSensor> dependencies) =>
            new ObservableMS5637(device);

        #endregion
    }
}
