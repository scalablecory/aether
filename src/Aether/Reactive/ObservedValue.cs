using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aether.Reactive
{
    /// <summary>
    /// Observes an <see cref="IObservable{T}"/>, exposing its latest value.
    /// </summary>
    /// <typeparam name="T">The type of value to observe.</typeparam>
    internal sealed class ObservedValue<T> : IObserver<T>
    {
        private readonly object _sync = new();
        private Exception? _exception;
        private T? _value;
        private Status _status;

        /// <summary>
        /// Gets the latest value published by the <see cref="IObserver{T}"/>, if any.
        /// </summary>
        /// <returns>If a value was retrieved, <see langword="true"/>. Otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Gets the latest value published by the <see cref="IObserver{T}"/>, if it hasn't already been retrieved.
        /// </summary>
        /// <returns>If a value was retrieved, <see langword="true"/>. Otherwise, <see langword="false"/>.</returns>
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
