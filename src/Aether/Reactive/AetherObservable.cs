using System.Reactive.Linq;

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
    }
}
