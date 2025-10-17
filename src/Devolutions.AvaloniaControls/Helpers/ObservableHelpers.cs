namespace Devolutions.AvaloniaControls.Helpers;

using Avalonia.Data;

public static class ObservableHelpers
{
    public static IBinding EmptyBinding() => new Binding { Source = null };

    public static IBinding ValueBinding(object? value) => new Binding { Source = value };
}