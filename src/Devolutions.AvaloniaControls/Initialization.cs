namespace Devolutions.AvaloniaControls;

using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;

public static class Initialization
{
    public static void Initialize()
    {
        EnableComboBoxTextValidation();
    }

    /// <summary>
    /// Enables data validation on ComboBox.TextProperty using reflection.
    /// This is a workaround for Avalonia not enabling validation by default on ComboBox.Text,
    /// unlike TextBox.Text which has enableDataValidation: true.
    ///
    /// This is a workaround until this is fixed in Avalonia: https://github.com/AvaloniaUI/Avalonia/issues/20462
    /// </summary>
    private static void EnableComboBoxTextValidation()
    {
        const BindingFlags nonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        // Get the _metadata dictionary from the property
        var metadataField = typeof(AvaloniaProperty).GetField("_metadata", nonPublicInstance);
        if (metadataField?.GetValue(ComboBox.TextProperty) is not IDictionary metadataDict)
        {
            return;
        }

        // Get the metadata for ComboBox
        if (!metadataDict.Contains(typeof(ComboBox)))
        {
            return;
        }

        var metadata = metadataDict[typeof(ComboBox)];
        if (metadata == null)
        {
            return;
        }

        // Set EnableDataValidation to true (auto-property backing field)
        var enableValidationField = typeof(AvaloniaPropertyMetadata).GetField(
            "<EnableDataValidation>k__BackingField",
            nonPublicInstance);
        enableValidationField?.SetValue(metadata, true);

        // Clear the metadata cache to ensure the change takes effect
        var cacheField = typeof(AvaloniaProperty).GetField("_metadataCache", nonPublicInstance);
        if (cacheField?.GetValue(ComboBox.TextProperty) is IDictionary cache)
        {
            cache.Clear();
        }
    }
}