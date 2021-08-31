using Aether.Devices.Sensors.Metadata;
using System.Reactive.Subjects;
using UnitsNet;

namespace Aether.Devices.Sensors.Observable
{
    /// <summary>
    /// A sensor which takes periodic measurements and can run commands.
    /// </summary>
    internal abstract class ObservableSensor : IAsyncDisposable
    {
        private readonly ReplaySubject<RelativeHumidity> _humidity = new(bufferSize: 1);
        private readonly ReplaySubject<Temperature> _temperature = new(bufferSize: 1);
        private readonly ReplaySubject<VolumeConcentration> _co2 = new(bufferSize: 1);
        private readonly ReplaySubject<Pressure> _pressure = new(bufferSize: 1);
        private readonly CancellationTokenSource _cts = new();
        private Task? _task;

        private object Sync => _cts;

        public IObservable<RelativeHumidity> RelativeHumidity
        {
            get
            {
                if (_task is null) StartIfNotStarted();
                return _humidity;
            }
        }

        public IObservable<Temperature> Temperature
        {
            get
            {
                if (_task is null) StartIfNotStarted();
                return _temperature;
            }
        }

        public IObservable<VolumeConcentration> CO2
        {
            get
            {
                if (_task is null) StartIfNotStarted();
                return _co2;
            }
        }

        public IObservable<Pressure> BarometricPressure
        {
            get
            {
                if (_task is null) StartIfNotStarted();
                return _pressure;
            }
        }

        public async ValueTask DisposeAsync()
        {            
            _cts.Cancel();

            Task? task;
            lock (Sync)
            {
                task = _task;
                _task = null;
            }

            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }

            DisposeCore();
            _humidity.Dispose();
            _temperature.Dispose();
            _co2.Dispose();
            _pressure.Dispose();

            GC.SuppressFinalize(this);
        }

        protected abstract void DisposeCore();

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNextRelativeHumidity(RelativeHumidity value) =>
            _humidity.OnNext(value);

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNextTemperature(Temperature value) =>
            _temperature.OnNext(value);

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNextCO2(VolumeConcentration value) =>
            _co2.OnNext(value);

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNextBarometricPressure(Pressure value) =>
            _pressure.OnNext(value);

        private async Task RunProcessLoopAsync()
        {
            CancellationToken cancellationToken = _cts.Token;

            // force the method creation to return a Task at this point, to avoid a long lock on Subscribe/Dispose race.
            await Task.Yield();

            try
            {
                await ProcessLoopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // do nothing.
            }
            catch (Exception ex)
            {
                _humidity.OnError(ex);
                _temperature.OnError(ex);
                _co2.OnError(ex);
                _pressure.OnError(ex);
                return;
            }

            _humidity.OnCompleted();
            _temperature.OnCompleted();
            _co2.OnCompleted();
            _pressure.OnCompleted();
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
        public ValueTask<object?> RunCommandAsync(SensorCommand command, object?[]? parameters, CancellationToken cancellationToken)
        {
            if (_task is null)
            {
                StartIfNotStarted();
            }

            return RunCommandAsyncCore(command, parameters, cancellationToken);
        }

        /// <summary>
        /// Runs a command against the sensor.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="parameters">Parameters to the command, if any.</param>
        /// <returns>The result of the command, if any.</returns>
        protected virtual ValueTask<object?> RunCommandAsyncCore(SensorCommand command, object?[]? parameters, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        private void StartIfNotStarted()
        {
            lock (Sync)
            {
                if (_task is null && !_cts.IsCancellationRequested)
                {
                    // This method will explicitly complete quickly and before running any code from a derived class.
                    _task = RunProcessLoopAsync();
                }
            }
        }
    }
}
