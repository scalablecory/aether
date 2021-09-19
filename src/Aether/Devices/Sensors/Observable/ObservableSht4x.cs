using Aether.Devices.Sensors.Metadata;
using System.Device.I2c;
using System.Reactive.Subjects;
using UnitsNet;

namespace Aether.Devices.Sensors.Observable
{
    internal sealed class ObservableSht4x : ObservableSensor, IObservableI2cSensorFactory
    {
        private readonly Drivers.Sht4x _sensor;
        private readonly ReplaySubject<RelativeHumidity> _rh = new(bufferSize: 1);
        private readonly ReplaySubject<Temperature> _t = new(bufferSize: 1);

        public override IObservable<RelativeHumidity> RelativeHumidity => _rh;
        public override IObservable<Temperature> Temperature => _t;

        private ObservableSht4x(I2cDevice device)
        {
            _sensor = new Drivers.Sht4x(device);
            Start();
        }

        protected override void DisposeCore()
        {
            _sensor.Dispose();
            _rh.Dispose();
            _t.Dispose();
        }

        protected override async Task ProcessLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            using var registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

            while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                (RelativeHumidity? humidity, Temperature? temperature) =
                    _sensor.ReadHumidityAndTemperature();

                if (humidity is not null) _rh.OnNext(humidity.GetValueOrDefault());
                if (temperature is not null) _t.OnNext(temperature.GetValueOrDefault());
            }
        }

        protected override void OnError(Exception ex)
        {
            _rh.OnError(ex);
            _t.OnError(ex);
        }

        protected override void OnCompleted()
        {
            _rh.OnCompleted();
            _t.OnCompleted();
        }

        #region IObservableI2CSensorFactory

        public static int DefaultAddress => Drivers.Sht4x.DefaultI2cAddress;

        public static string Manufacturer => "Sensirion";

        public static string Name => "SHT4x";

        public static string Uri => "https://www.sensirion.com/en/environmental-sensors/humidity-sensors/humidity-sensor-sht4x/";

        public static IEnumerable<MeasureInfo> Measures { get; } = new[]
        {
            new MeasureInfo(Measure.Humidity),
            new MeasureInfo(Measure.Temperature)
        };

        public static IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public static IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public static ObservableSensor OpenSensor(I2cDevice device, IEnumerable<ObservableSensor> dependencies) =>
            new ObservableSht4x(device);

        #endregion
    }
}
