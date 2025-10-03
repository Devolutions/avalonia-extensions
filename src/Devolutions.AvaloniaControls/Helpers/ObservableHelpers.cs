namespace Devolutions.AvaloniaControls.Helpers;

using Avalonia.Data;

public static class ObservableHelpers
{
    public static IBinding EmptyBinding() => new Binding { Source = null };
    public static IBinding ValueBinding(object? value) => new Binding { Source = value };
}

public class SingleValueObservable<T>(T value) : IObservable<T>
{
    public IDisposable Subscribe(IObserver<T> observer)
    {
        observer.OnNext(value);
        observer.OnCompleted();
        return EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        private EmptyDisposable() { }

        public void Dispose() { }
    }
}