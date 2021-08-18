using System.Diagnostics;
using System.Reactive.Subjects;

namespace Aether.Devices
{
    internal abstract class Sensor : IObservable<Measurement>, IAsyncDisposable
    {
        private readonly Subject<Measurement> _subject = new Subject<Measurement>();
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _cancellationToken;
        private Task? _task;

        protected Sensor()
        {
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }

            if (_task is not null)
            {
                await _task.ConfigureAwait(false);
            }
        }

        protected abstract ValueTask DisposeAsyncCore();

        public IDisposable Subscribe(IObserver<Measurement> observer) =>
            _subject.Subscribe(observer);

        public void Start()
        {
            Debug.Assert(_task is null);
            _task = ProcessAsync();
        }

        private async Task ProcessAsync()
        {
            using (_subject)
            using (_cts)
            {
                try
                {
                    IAsyncEnumerator<Measurement> e = GetMeasurementsAsync(_cancellationToken);
                    await using (e.ConfigureAwait(false))
                    {
                        while (await e.MoveNextAsync().ConfigureAwait(false))
                        {
                            _subject.OnNext(e.Current);
                        }
                    }
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == _cancellationToken)
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
        }

        protected abstract IAsyncEnumerator<Measurement> GetMeasurementsAsync(CancellationToken cancellationToken);
    }
}
