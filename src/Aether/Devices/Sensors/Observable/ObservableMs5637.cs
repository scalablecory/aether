using Aether.Devices.Sensors.Metadata;
using System.Device.I2c;
using System.Reactive.Subjects;
using UnitsNet;

namespace Aether.Devices.Sensors.Observable
{
    internal sealed class ObservableMs5637 : ObservableSensor, IObservableI2cSensorFactory, ISimulatedI2cDeviceFactory
    {
        private readonly Drivers.Ms5637 _sensor;
        private readonly ReplaySubject<Pressure> _p = new(bufferSize: 1);
        private readonly ReplaySubject<Temperature> _t = new(bufferSize: 1);

        public override IObservable<Pressure> BarometricPressure => _p;
        public override IObservable<Temperature> Temperature => _t;

        private ObservableMs5637(I2cDevice device)
        {
            _sensor = new Drivers.Ms5637(device);
        }

        protected override void DisposeCore()
        {
            _sensor.Dispose();
            _p.Dispose();
            _t.Dispose();
        }

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                (Temperature temperature, Pressure pressure) =
                    _sensor.ReadTemperatureAndPressure();

                _t.OnNext(temperature);
                _p.OnNext(pressure);
            }
        }

        protected override void OnError(Exception ex)
        {
            _t.OnError(ex);
            _p.OnError(ex);
        }

        protected override void OnCompleted()
        {
            _t.OnCompleted();
            _p.OnCompleted();
        }

        #region IObservableI2CSensorFactory

        public static int DefaultAddress => Drivers.Ms5637.DefaultI2cAddress;

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

        public static ObservableSensor OpenSensor(I2cDevice device, IEnumerable<ObservableSensor> dependencies) =>
            new ObservableMs5637(device);

        public static I2cDevice CreateSimulatedI2cDevice() =>
            new Simulated.SimulatedMS5637();

        #endregion
    }
}
