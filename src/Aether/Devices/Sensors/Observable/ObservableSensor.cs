using Aether.Devices.Sensors.Metadata;
using System.Reactive.Subjects;

namespace Aether.Devices.Sensors.Observable
{
    internal abstract class ObservableSensor : IObservable<Measurement>, IAsyncDisposable
    {
        private readonly Subject<Measurement> _subject = new();
        private readonly CancellationTokenSource _cts = new();
        private Task? _task;

        private object Sync => _cts;

        public virtual bool CanCalibrate => false;

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
            _subject.Dispose();

            GC.SuppressFinalize(this);
        }

        protected abstract void DisposeCore();

        public IDisposable Subscribe(IObserver<Measurement> observer)
        {
            if (_task is null)
            {
                StartIfNotStarted();
            }

            return _subject.Subscribe(observer);
        }

        /// <summary>
        /// Called by <see cref="ProcessLoopAsync(CancellationToken)"/> to report a new periodic measurement.
        /// </summary>
        /// <param name="measure">The type of measure being reported.</param>
        /// <param name="value">The value of the measure being reported.</param>
        protected void OnNext(Measure measure, float value) =>
            _subject.OnNext(new Measurement(measure, value));

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
                _subject.OnError(ex);
                return;
            }

            _subject.OnCompleted();
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
