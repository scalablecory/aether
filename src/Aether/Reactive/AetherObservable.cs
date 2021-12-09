using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Channels;

namespace Aether.Reactive
{
    internal static class AetherObservable
    {
        /// <summary>
        /// For every subscription, creates an <see cref="IAsyncDisposable"/> resource and publishes an <see cref="IObservable{T}"/> from it.
        /// The <see cref="IAsyncDisposable"/> will be disposed after the <see cref="IObservable{T}"/> finishes executing but before any exceptions are observed.
        /// </summary>
        /// <typeparam name="TResource">The type of resource to operate on.</typeparam>
        /// <typeparam name="TResult">The element type of the <see cref="IObservable{T}"/> to return.</typeparam>
        /// <param name="createResource">A function to create an <see cref="IAsyncDisposable"/> resource.</param>
        /// <param name="selectElements">A function to select an <see cref="IObservable{T}"/> sequence from the resource created by <paramref name="createResource"/>.</param>
        public static IObservable<TResult> AsyncUsing<TResource, TResult>(Func<TResource> createResource, Func<TResource, IObservable<TResult>> selectElements)
            where TResource : IAsyncDisposable
            =>
            Observable.Defer(() => Observable.Return(createResource()))
                .SelectMany(resource => selectElements(resource).Finally(() => resource.DisposeAsync().AsTask()));

        /// <summary>
        /// For every subscription, creates an <see cref="IAsyncDisposable"/> resource and publishes an <see cref="IObservable{T}"/> from it.
        /// The <see cref="IAsyncDisposable"/> will be disposed after the <see cref="IObservable{T}"/> finishes executing but before any exceptions are observed.
        /// </summary>
        /// <typeparam name="TResource">The type of resource to operate on.</typeparam>
        /// <typeparam name="TResult">The element type of the <see cref="IObservable{T}"/> to return.</typeparam>
        /// <param name="createResource">A function to create an <see cref="IAsyncDisposable"/> resource.</param>
        /// <param name="selectElements">A function to select an <see cref="IObservable{T}"/> sequence from the resource created by <paramref name="createResource"/>.</param>
        public static IObservable<TResult> AsyncUsing<TResource, TResult>(Func<CancellationToken, Task<TResource>> createResource, Func<TResource, IObservable<TResult>> selectElements)
            where TResource : IAsyncDisposable
            =>
            Observable.FromAsync(createResource)
                .SelectMany(resource => selectElements(resource).Finally(() => resource.DisposeAsync().AsTask()));

        /// <summary>
        /// Executes an asynchronous function after an <see cref="IObservable{T}"/> terminates.
        /// The <paramref name="finallyFunc"/> function will be executed after <paramref name="observable"/> finishes executing but before any exceptions are observed.
        /// </summary>
        /// <param name="observable">An observable sequence.</param>
        /// <param name="finallyFunc">The function to execute after <paramref name="observable"/> completes.</param>
        public static IObservable<T> Finally<T>(this IObservable<T> observable, Func<Task> finallyFunc) =>
            observable.Finally(Observable.FromAsync(finallyFunc).IgnoreElements().Select(static _ => default(T)!));

        /// <summary>
        /// Appends <paramref name="finallySequence"/> to <paramref name="observable"/>.
        /// The <paramref name="finallySequence"/> sequence will be observed after <paramref name="observable"/> finishes executing but before any exceptions are observed.
        /// </summary>
        /// <param name="observable">An observable sequence.</param>
        /// <param name="finallySequence">The observable sequence to append to <paramref name="observable"/>.</param>
        public static IObservable<T> Finally<T>(this IObservable<T> observable, IObservable<T> finallySequence) =>
            observable
                .Catch<T, Exception>(exception => Observable.Concat(finallySequence, Observable.Throw<T>(exception)))
                .Concat(finallySequence);

        /// <summary>
        /// Buffers observed items until the observer is ready to be called.
        /// </summary>
        /// <param name="observable">An observable sequence.</param>
        /// <remarks>
        /// This is useful when an observer takes a long time to process, but can process any number of items in that time.
        /// </remarks>
        public static IObservable<IList<T>> Gate<T>(this IObservable<T> observable) =>
            Observable.Create(async (IObserver<IList<T>> observer, CancellationToken cancellationToken) =>
            {
                Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
                {
                    SingleReader = true
                });

                using CancellationTokenRegistration reg = cancellationToken.UnsafeRegister(static obj => ((ChannelWriter<T>)obj!).TryComplete(), channel.Writer);

                using IDisposable sub = observable.Subscribe(next =>
                {
                    try
                    {
                        channel.Writer.TryWrite(next);
                    }
                    catch (ChannelClosedException)
                    {
                        // ignore.
                    }
                },
                err =>
                {
                    channel.Writer.TryComplete(err);
                },
                () =>
                {
                    channel.Writer.TryComplete();
                });

                var list = new List<T>();

                do
                {
                    while (channel.Reader.TryRead(out T? value))
                    {
                        list.Add(value);
                    }

                    if (list.Count != 0)
                    {
                        observer.OnNext(list);
                        list = new List<T>();
                    }
                }
                while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false));
            });

        /// <summary>
        /// Gets an event that triggers when the console's cancel key (CTRL+C) is pressed is pressed.
        /// </summary>
        public static IObservable<Unit> ConsoleCancelKeyPress { get; } =
            Observable.FromEventPattern<ConsoleCancelEventHandler, ConsoleCancelEventArgs>(eh => Console.CancelKeyPress += eh, eh => Console.CancelKeyPress -= eh)
            .Select(evt =>
            {
                evt.EventArgs.Cancel = true;
                return Unit.Default;
            })
            .Publish()
            .AutoConnect();
    }
}
