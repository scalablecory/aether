using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aether
{
    internal sealed class ObservedValue<T> : IObserver<T>, IDisposable
    {
        private readonly object _sync = new object();
        private readonly IDisposable _subscription;
        private Status _status;
        private T? _value;
        private Exception? _exception;

        public ObservedValue(IObservable<T> observable)
        {
            _subscription = observable.Subscribe(this);
        }

        public void Dispose() =>
            _subscription.Dispose();

        public bool TryGetValue([MaybeNullWhen(false)] out T value)
        {
            lock (_sync)
            {
                switch (_status & Status.MainStateMask)
                {
                    default:
                    case Status.NoValue:
                        Debug.Assert(_status == Status.NoValue, "Unhandled status.");
                        value = default;
                        return false;
                    case Status.HasChanged:
                        _status = (_status & ~Status.MainStateMask) | Status.HasValue;
                        goto case Status.HasValue;
                    case Status.HasValue:
                        Debug.Assert(_value is not null);
                        value = _value;
                        return true;
                    case Status.HasError:
                        Debug.Assert(_exception is not null);
                        throw new AggregateException(_exception);
                }
            }
        }

        public bool TryGetValueIfChanged([MaybeNullWhen(false)] out T value)
        {
            lock (_sync)
            {
                switch (_status & Status.MainStateMask)
                {
                    default:
                        Debug.Assert(_status == Status.NoValue, "Unhandled status.");
                        goto case Status.NoValue;
                    case Status.NoValue:
                    case Status.HasValue:
                        value = default;
                        return false;
                    case Status.HasChanged:
                        Debug.Assert(_value is not null);
                        _status = (_status & ~Status.MainStateMask) | Status.HasValue;
                        value = _value;
                        return true;
                    case Status.HasError:
                        Debug.Assert(_exception is not null);
                        throw new AggregateException(_exception);
                }
            }
        }

        void IObserver<T>.OnCompleted()
        {
            lock (_sync)
            {
                _status &= Status.IsCompleted;
            }
        }

        void IObserver<T>.OnError(Exception error)
        {
            lock (_sync)
            {
                _status = Status.HasError;
                _exception = error;
            }
        }

        void IObserver<T>.OnNext(T value)
        {
            lock (_sync)
            {
                _value = value;
                _status = Status.HasChanged;
            }
        }

        [Flags]
        private enum Status
        {
            NoValue = 0,
            HasValue = 1,
            HasChanged = 2,
            HasError = 3,
            IsCompleted = 4,

            MainStateMask = NoValue | HasValue | HasChanged | HasError
        }
    }
}
