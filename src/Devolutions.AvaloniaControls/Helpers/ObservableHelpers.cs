namespace Devolutions.AvaloniaControls.Helpers;

using Avalonia.Data;

public static class ObservableHelpers
{
    public static BindingBase EmptyBinding() => new Binding { Source = null };

    public static BindingBase ValueBinding(object? value) => new Binding { Source = value };
}