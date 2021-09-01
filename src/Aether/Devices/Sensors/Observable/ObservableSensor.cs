using Aether.Devices.Sensors.Metadata;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using UnitsNet;
using O = System.Reactive.Linq.Observable;

namespace Aether.Devices.Sensors.Observable
{
    /// <summary>
    /// A sensor which takes periodic measurements and can run commands.
    /// </summary>
    internal abstract class ObservableSensor : IAsyncDisposable
    {
        private readonly ReplaySubject<RelativeHumidity>? _humidity;
        private readonly ReplaySubject<Temperature>? _temperature;
        private readonly ReplaySubject<VolumeConcentration>? _co2;
        private readonly ReplaySubject<Pressure>? _pressure;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _task;
        private TaskCompletionSource? _startTaskTcs = new();

        public IObservable<RelativeHumidity> RelativeHumidity
        {
            get
            {
                StartIfNotStarted();
                return _humidity ?? O.Empty<RelativeHumidity>();
            }
        }

        public IObservable<Temperature> Temperature
        {
            get
            {
                StartIfNotStarted();
                return _temperature ?? O.Empty<Temperature>();
            }
        }

        public IObservable<VolumeConcentration> CO2
        {
            get
            {
                StartIfNotStarted();
                return _co2 ?? O.Empty<VolumeConcentration>();
            }
        }

        public IObservable<Pressure> BarometricPressure
        {
            get
            {
                StartIfNotStarted();
                return _pressure ?? O.Empty<Pressure>();
            }
        }

        protected ObservableSensor(params Measure[] measures)
        {
            foreach (Measure measure in measures)
            {
                switch (measure)
                {
                    case Measure.Humidity:
                        _humidity = new ReplaySubject<RelativeHumidity>(1);
                        break;
                    case Measure.Temperature:
                        _temperature = new ReplaySubject<Temperature>(1);
                        break;
                    case Measure.CO2:
                        _co2 = new ReplaySubject<VolumeConcentration>(1);
                        break;
                    case Measure.Pressure:
                        _pressure = new ReplaySubject<Pressure>(1);
                        break;
                }
            }

            _task = RunProcessLoopAsync();
        }

        public async ValueTask DisposeAsync()
        {            
            _cts.Cancel();
            _startTaskTcs?.TrySetResult();
            await _task.ConfigureAwait(false);

            DisposeCore();
            _humidity?.Dispose();
            _temperature?.Dispose();
            _co2?.Dispose();
            _pressure?.Dispose();

            GC.SuppressFinalize(this);
        }

        protected abstract void DisposeCore();

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNextRelativeHumidity(RelativeHumidity value)
        {
            if (_humidity is null) ThrowInvalidOnNext();
            _humidity.OnNext(value);
        }

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNextTemperature(Temperature value)
        {
            if (_temperature is null) ThrowInvalidOnNext();
            _temperature.OnNext(value);
        }

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNextCO2(VolumeConcentration value)
        {
            if (_co2 is null) ThrowInvalidOnNext();
            _co2.OnNext(value);
        }

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNextBarometricPressure(Pressure value)
        {
            if (_pressure is null) ThrowInvalidOnNext();
            _pressure.OnNext(value);
        }

        [DoesNotReturn]
        private static void ThrowInvalidOnNext([CallerMemberName] string? methodName = null) =>
            throw new InvalidOperationException($"Call to {methodName} is not valid; measure not specified in {nameof(ObservableSensor)} constructor.");

        private void StartIfNotStarted() =>
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
                _humidity?.OnError(ex);
                _temperature?.OnError(ex);
                _co2?.OnError(ex);
                _pressure?.OnError(ex);
                return;
            }

            _humidity?.OnCompleted();
            _temperature?.OnCompleted();
            _co2?.OnCompleted();
            _pressure?.OnCompleted();
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
    }
}
