namespace Devolutions.AvaloniaControls.Behaviors;

using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

/// <summary>
/// Behavior that enables data validation on ComboBox.Text bindings.
/// Works around Avalonia's lack of built-in validation support for ComboBox.Text
/// by manually extracting the binding path and subscribing to INotifyDataErrorInfo.
///
/// This is a work-around until this is implemented: https://github.com/AvaloniaUI/Avalonia/issues/20462
/// </summary>
public static class ComboBoxValidationBehavior
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("Enable", typeof(ComboBoxValidationBehavior));

    private static readonly ConditionalWeakTable<ComboBox, ValidationState> States = new();

    static ComboBoxValidationBehavior()
    {
        EnableProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is ComboBox comboBox)
            {
                var enable = args.NewValue.GetValueOrDefault<bool>();
                if (enable)
                {
                    EnableValidation(comboBox);
                }
                else
                {
                    DisableValidation(comboBox);
                }
            }
        });
    }

    public static void SetEnable(ComboBox element, bool value) => element.SetValue(EnableProperty, value);

    public static bool GetEnable(ComboBox element) => element.GetValue(EnableProperty);

    private static void EnableValidation(ComboBox comboBox)
    {
        var state = new ValidationState(comboBox);
        States.AddOrUpdate(comboBox, state);

        // Subscribe to DataContext changes to handle dynamic DataContext
        comboBox.DataContextChanged += OnDataContextChanged;

        // Subscribe to when the control is fully loaded to get binding info
        if (comboBox.IsLoaded)
        {
            SetupValidation(comboBox, state);
        }
        else
        {
            comboBox.Loaded += OnLoaded;
        }

        // Cleanup when control is unloaded
        comboBox.DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private static void DisableValidation(ComboBox comboBox)
    {
        if (States.TryGetValue(comboBox, out var state))
        {
            state.Dispose();
            States.Remove(comboBox);
        }

        comboBox.DataContextChanged -= OnDataContextChanged;
        comboBox.Loaded -= OnLoaded;
        comboBox.DetachedFromVisualTree -= OnDetachedFromVisualTree;
    }

    private static void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox && States.TryGetValue(comboBox, out var state))
        {
            comboBox.Loaded -= OnLoaded;
            SetupValidation(comboBox, state);
        }
    }

    private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            DisableValidation(comboBox);
        }
    }

    private static void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (sender is ComboBox comboBox && States.TryGetValue(comboBox, out var state))
        {
            // Re-setup validation with the new DataContext
            state.UnsubscribeFromDataContext();
            SetupValidation(comboBox, state);
        }
    }

    private static void SetupValidation(ComboBox comboBox, ValidationState state)
    {
        // Get the binding path for ComboBox.TextProperty
        var bindingPath = GetBindingPath(comboBox, ComboBox.TextProperty);
        if (string.IsNullOrEmpty(bindingPath))
        {
            return; // No binding on Text property
        }

        state.BindingPath = bindingPath;

        // Subscribe to INotifyDataErrorInfo on DataContext
        if (comboBox.DataContext is INotifyDataErrorInfo ndei)
        {
            state.SubscribeToDataContext(ndei, comboBox);
        }
    }

    /// <summary>
    /// Extracts the binding path from an Avalonia property using reflection.
    /// </summary>
    private static string? GetBindingPath(AvaloniaObject target, AvaloniaProperty property)
    {
        try
        {
            // Get the ValueStore via reflection
            // AvaloniaObject has: internal ValueStore GetValueStore() => _values;
            var getValueStoreMethod = typeof(AvaloniaObject).GetMethod(
                "GetValueStore",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var valueStore = getValueStoreMethod?.Invoke(target, null);
            if (valueStore == null)
            {
                return null;
            }

            // ValueStore has: public BindingExpressionBase? GetExpression(AvaloniaProperty property)
            var getExpressionMethod = valueStore.GetType().GetMethod(
                "GetExpression",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                [typeof(AvaloniaProperty)],
                null);

            var expression = getExpressionMethod?.Invoke(valueStore, [property]);
            if (expression == null)
            {
                return null;
            }

            // BindingExpressionBase has: public abstract string Description { get; }
            var descriptionProperty = expression.GetType().GetProperty(
                "Description",
                BindingFlags.Instance | BindingFlags.Public);

            var description = descriptionProperty?.GetValue(expression) as string;

            // Description might be something like "RequiredTextValue" or "Model.Property"
            // For simple cases, this is directly the property name
            return description;
        }
        catch
        {
            // If reflection fails, return null (graceful degradation)
            return null;
        }
    }

    private sealed class ValidationState : IDisposable
    {
        private readonly ComboBox _comboBox;
        private INotifyDataErrorInfo? _currentDataErrorInfo;
        private EventHandler<DataErrorsChangedEventArgs>? _errorsChangedHandler;

        public ValidationState(ComboBox comboBox)
        {
            this._comboBox = comboBox;
        }

        public string? BindingPath { get; set; }

        public void SubscribeToDataContext(INotifyDataErrorInfo ndei, ComboBox comboBox)
        {
            this.UnsubscribeFromDataContext();

            this._currentDataErrorInfo = ndei;
            this._errorsChangedHandler = (sender, args) => this.OnErrorsChanged(args, comboBox);
            ndei.ErrorsChanged += this._errorsChangedHandler;

            // Check for existing errors immediately
            this.UpdateValidationErrors(comboBox);
        }

        public void UnsubscribeFromDataContext()
        {
            if (this._currentDataErrorInfo != null && this._errorsChangedHandler != null)
            {
                this._currentDataErrorInfo.ErrorsChanged -= this._errorsChangedHandler;
            }

            this._currentDataErrorInfo = null;
            this._errorsChangedHandler = null;
        }

        private void OnErrorsChanged(DataErrorsChangedEventArgs args, ComboBox comboBox)
        {
            // Only update if the changed property matches our binding path
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == this.BindingPath)
            {
                this.UpdateValidationErrors(comboBox);
            }
        }

        private void UpdateValidationErrors(ComboBox comboBox)
        {
            if (this._currentDataErrorInfo == null || string.IsNullOrEmpty(this.BindingPath))
            {
                return;
            }

            var errors = this._currentDataErrorInfo.GetErrors(this.BindingPath);
            var errorList = errors?.Cast<object>().ToList() ?? [];

            if (errorList.Count > 0)
            {
                // Set the first error (most common pattern)
                var firstError = errorList[0];
                var exception = firstError is Exception ex
                    ? ex
                    : new DataValidationException(firstError?.ToString() ?? "Validation error");

                DataValidationErrors.SetError(comboBox, exception);
            }
            else
            {
                DataValidationErrors.SetError(comboBox, null);
            }
        }

        public void Dispose()
        {
            this.UnsubscribeFromDataContext();
            DataValidationErrors.SetError(this._comboBox, null);
        }
    }
}