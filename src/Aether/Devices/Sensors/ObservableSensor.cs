using Aether.CustomUnits;
using Aether.Devices.Drivers;
using Aether.Devices.Sensors.Metadata;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using UnitsNet;

namespace Aether.Devices.Sensors
{
    /// <summary>
    /// A sensor which takes periodic measurements and can run commands.
    /// </summary>
    internal abstract class ObservableSensor : IObservable<Measurement>, IAsyncDisposable
    {
        private readonly Subject<Measurement> _measurements = new Subject<Measurement>();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _task;
        private TaskCompletionSource? _startTaskTcs = new();

        protected ObservableSensor()
        {
            _task = RunProcessLoopAsync();
        }

        public async ValueTask DisposeAsync()
        {            
            _cts.Cancel();
            _startTaskTcs?.TrySetResult();
            await _task.ConfigureAwait(false);

            DisposeCore();

            GC.SuppressFinalize(this);
        }

        protected abstract void DisposeCore();

        protected void Start() =>
            Interlocked.Exchange(ref _startTaskTcs, null)?.TrySetResult();

        private async Task RunProcessLoopAsync()
        {
            // It's important that this completes asynchronously, to ensure it returns an
            // incomplete Task. This is called from the ctor, and we don't want to call
            // the derived sensor's ProcessLoopAsync before running the derived ctor.
            await _startTaskTcs!.Task.ConfigureAwait(false);

            try
            {
                await ProcessLoopAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == _cts.Token)
            {
                // do nothing.
            }
            catch (Exception ex)
            {
                _measurements.OnError(ex);
                return;
            }

            _measurements.OnCompleted();
        }

        /// <summary>
        /// The main process loop, which should periodically call <see cref="OnNext(Measurement)"/> with new measurements.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token indicating when the <see cref="ObservableSensor"/> is disposed.</param>
        protected abstract Task ProcessLoopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Runs a command against the sensor.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="parameters">Parameters to the command, if any.</param>
        /// <returns>The result of the command, if any.</returns>
        public ValueTask<object?> RunCommandAsync(SensorCommand command, object?[]? parameters, CancellationToken cancellationToken) =>
            RunCommandAsyncCore(command, parameters, cancellationToken);

        /// <summary>
        /// Runs a command against the sensor.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="parameters">Parameters to the command, if any.</param>
        /// <returns>The result of the command, if any.</returns>
        protected virtual ValueTask<object?> RunCommandAsyncCore(SensorCommand command, object?[]? parameters, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public IDisposable Subscribe(IObserver<Measurement> observer) =>
            _measurements.Subscribe(observer);

        protected void OnNextRelativeHumidity(RelativeHumidity h) =>
            _measurements.OnNext(Measurement.FromRelativeHumidity(h));

        protected void OnNextTemperature(Temperature t) =>
            _measurements.OnNext(Measurement.FromTemperature(t));

        protected void OnNextCo2(VolumeConcentration co2) =>
            _measurements.OnNext(Measurement.FromCo2(co2));

        protected void OnNextBarometricPressure(Pressure p) =>
            _measurements.OnNext(Measurement.FromPressure(p));

        protected void OnNextVolitileOrganicCompound(VolatileOrganicCompoundIndex vocIndex) =>
            _measurements.OnNext(Measurement.FromVoc(vocIndex));

        protected void OnNextParticulate1_0PMassConcentrationMeasurement(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.From1_0PMassConcentration(particulateData.PM1_0));

        protected void OnNextParticulate2_5PMassConcentrationMeasurement(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.From2_5PMassConcentration(particulateData.PM2_5));

        protected void OnNextParticulate4_0PMassConcentrationMeasurement(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.From4_0PMassConcentration(particulateData.PM4_0));

        protected void OnNextParticulate10_0PMassConcentrationMeasurement(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.From10_0PMassConcentration(particulateData.PM10_0));

        protected void OnNextParticulate0_5PNumberConcentrationMeasurement(Sps30ParticulateData particulateData) =>
         _measurements.OnNext(Measurement.From0_5NumberConcentration(particulateData.P0_5));

        protected void OnNextParticulate1_0PNumberConcentrationMeasurement(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.From1_0NumberConcentration(particulateData.P1_0));

        protected void OnNextParticulate2_5PNumberConcentrationMeasurement(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.From2_5NumberConcentration(particulateData.P2_5));

        protected void OnNextParticulate4_0PNumberConcentrationMeasurement(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.From4_0NumberConcentration(particulateData.P4_0));

        protected void OnNextParticulate10_0PNumberConcentrationMeasurement(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.From10_0NumberConcentration(particulateData.P10_0));

        protected void OnNextParticulateTypicalSize(Sps30ParticulateData particulateData) =>
            _measurements.OnNext(Measurement.FromParticulateTypicalSize(particulateData.TypicalParticleSize));
    }
}
