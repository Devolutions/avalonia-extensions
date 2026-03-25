namespace Devolutions.AvaloniaControls.Helpers;

using Avalonia;
using Avalonia.Data;

public static class ObservableHelpers
{
    public static IBinding EmptyBinding() => SingleValueObservable<object?>.Null.ToBinding();

    public static IBinding ValueBinding(object? value) => new SingleValueObservable<object?>(value).ToBinding();

    /// <summary>
    /// A minimal IObservable that emits a single value immediately on subscribe.
    /// Avoids dependency on System.Reactive while remaining fully AOT-safe.
    /// </summary>
    private sealed class SingleValueObservable<T>(T value) : IObservable<T>
    {
        public static readonly SingleValueObservable<T> Null = new(default!);

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(value);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }

    private static class Disposable
    {
        public static readonly IDisposable Empty = new EmptyDisposable();

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}