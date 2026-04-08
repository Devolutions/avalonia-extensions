namespace Devolutions.AvaloniaControls.Behaviors;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
public class ComboBoxValidationBehavior : BaseBehavior<ComboBoxValidationBehavior, ComboBox>
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("Enable", typeof(ComboBoxValidationBehavior));

    static ComboBoxValidationBehavior()
    {
        RegisterBehaviorProperty(EnableProperty);
    }
    
    private INotifyDataErrorInfo? currentDataErrorInfo;
    private EventHandler<DataErrorsChangedEventArgs>? errorsChangedHandler;
    

    protected override void OnLoaded()
    {
        SetupValidation();
    }
    
    protected override void OnDataContextChanged()
    {
        this.UnsubscribeFromDataContext();
        SetupValidation();
    }

    private void SetupValidation()
    {
        // Get the binding path for ComboBox.TextProperty
        var bindingPath = GetBindingPath(this.Target, ComboBox.TextProperty);
        if (string.IsNullOrEmpty(bindingPath))
        {
            return; // No binding on Text property
        }

        this.BindingPath = bindingPath;

        // Subscribe to INotifyDataErrorInfo on DataContext
        if (this.Target.DataContext is INotifyDataErrorInfo ndei)
        {
            this.SubscribeToDataContext(ndei);
        }
    }

    /// <summary>
    /// Extracts the binding path from an Avalonia property using reflection.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming",
        "IL2075",
        Justification =
            "Accesses Avalonia internals (GetValueStore, GetExpression, Description) via reflection. These types are preserved via assembly-level ILLink descriptor (preserve=\"all\") and by Avalonia's own trimming guarantees for core types.")]
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

    public string? BindingPath { get; set; }

    public void SubscribeToDataContext(INotifyDataErrorInfo ndei)
    {
        this.UnsubscribeFromDataContext();

        this.currentDataErrorInfo = ndei;
        this.errorsChangedHandler = (_, args) => this.OnErrorsChanged(args, this.Target);
        ndei.ErrorsChanged += this.errorsChangedHandler;

        // Check for existing errors immediately
        this.UpdateValidationErrors(this.Target);
    }

    public void UnsubscribeFromDataContext()
    {
        if (this.currentDataErrorInfo != null && this.errorsChangedHandler != null)
        {
            this.currentDataErrorInfo.ErrorsChanged -= this.errorsChangedHandler;
        }

        this.currentDataErrorInfo = null;
        this.errorsChangedHandler = null;
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
        if (this.currentDataErrorInfo == null || string.IsNullOrEmpty(this.BindingPath))
        {
            return;
        }

        var errors = this.currentDataErrorInfo.GetErrors(this.BindingPath);
        var errorList = errors.Cast<object>().ToList();

        if (errorList.Count > 0)
        {
            // Set the first error (most common pattern)
            var firstError = errorList[0];
            var exception = firstError as Exception ?? new DataValidationException(firstError?.ToString() ?? "Validation error");

            DataValidationErrors.SetError(comboBox, exception);
        }
        else
        {
            DataValidationErrors.SetError(comboBox, null);
        }
    }

    public override void Dispose()
    {
        this.UnsubscribeFromDataContext();
        DataValidationErrors.SetError(this.Target, null);
    }
}