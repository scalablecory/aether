using System.Device.I2c;
using System.Reactive.Linq;
using Aether.Devices.Drivers;
using Aether.Devices.Sensors.Metadata;

namespace Aether.Devices.Sensors
{
    internal sealed class ObservableSps30 : I2cSensorFactory
    {
        public static ObservableSps30 Instance { get; } = new ObservableSps30();

        public override int DefaultAddress => Sps30.DefaultI2cAddress;

        public override string Manufacturer => "Sensirion";

        public override string Name => "SPS30";

        public override string Uri => "https://www.sensirion.com/en/environmental-sensors/particulate-matter-sensors-pm25/";

        public override IEnumerable<MeasureInfo> Measures { get; } = new[]
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

        public override IEnumerable<SensorDependency> Dependencies => SensorDependency.NoDependencies;
        public override IEnumerable<SensorCommand> Commands => SensorCommand.NoCommands;

        public override IObservable<Measurement> OpenSensor(Func<I2cDevice> deviceFunc, IObservable<Measurement> dependencies) =>
            Observable.Create(async (IObserver<Measurement> measurements, CancellationToken cancellationToken) =>
            {
                using I2cDevice device = deviceFunc();
                using var sensor = new Sps30(device);
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
                using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static @timer => ((PeriodicTimer)@timer!).Dispose(), timer);

                sensor.StartMeasurement();

                try
                {
                    while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
                    {
                        bool? sensorDataReady = sensor.CheckSensorDataReady();

                        if (sensorDataReady is not null && sensorDataReady.Value)
                        {
                            Sps30ParticulateData particulateData = sensor.ReadMeasuredValues();

                            if (particulateData.PM1_0 is not null) measurements.OnNext(Measurement.FromPM1_0(particulateData.PM1_0.GetValueOrDefault()));
                            if (particulateData.PM2_5 is not null) measurements.OnNext(Measurement.FromPM2_5(particulateData.PM2_5.GetValueOrDefault()));
                            if (particulateData.PM4_0 is not null) measurements.OnNext(Measurement.FromPM4_0(particulateData.PM4_0.GetValueOrDefault()));
                            if (particulateData.PM10_0 is not null) measurements.OnNext(Measurement.FromPM10_0(particulateData.PM10_0.GetValueOrDefault()));
                            if (particulateData.P0_5 is not null) measurements.OnNext(Measurement.FromP0_5(particulateData.P0_5.GetValueOrDefault()));
                            if (particulateData.P1_0 is not null) measurements.OnNext(Measurement.FromP1_0(particulateData.P1_0.GetValueOrDefault()));
                            if (particulateData.P2_5 is not null) measurements.OnNext(Measurement.FromP2_5(particulateData.P2_5.GetValueOrDefault()));
                            if (particulateData.P4_0 is not null) measurements.OnNext(Measurement.FromP4_0(particulateData.P4_0.GetValueOrDefault()));
                            if (particulateData.P10_0 is not null) measurements.OnNext(Measurement.FromP10_0(particulateData.P10_0.GetValueOrDefault()));
                            if (particulateData.TypicalParticleSize is not null) measurements.OnNext(Measurement.FromTypicalParticleSize(particulateData.TypicalParticleSize.GetValueOrDefault()));
                        }
                    }
                }
                finally
                {
                    sensor.StopMeasurement();
                }
            });
    }
}
