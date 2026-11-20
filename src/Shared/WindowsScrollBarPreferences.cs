namespace Devolutions.AvaloniaTheme.Internal;

using System.Globalization;
using System.Security;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Microsoft.Win32;

internal static class WindowsScrollBarPreferences
{
    internal static void Apply(IResourceDictionary resources, double defaultSize, double defaultThumbSize,
        Func<string, object?>? readValue = null, bool insetThumb = false)
    {
        if (readValue is not null)
        {
            ApplyValues(readValue("ScrollWidth"), readValue("ScrollHeight"));
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics");
            ApplyValues(key?.GetValue("ScrollWidth"), key?.GetValue("ScrollHeight"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            Warn("Cannot read Windows scrollbar preferences: {Error}", ex.Message);
        }

        void ApplyValues(object? width, object? height)
        {
            double verticalScale = GetScale(width, "ScrollWidth");
            double horizontalScale = GetScale(height, "ScrollHeight");
            resources["ScrollBarVerticalSize"] = defaultSize * verticalScale;
            resources["ScrollBarHorizontalSize"] = defaultSize * horizontalScale;
            resources[insetThumb ? "ScrollBarVerticalThumbMinWidth" : "ScrollBarVerticalThumbSize"] = defaultThumbSize * verticalScale;
            resources[insetThumb ? "ScrollBarHorizontalThumbMinHeight" : "ScrollBarHorizontalThumbSize"] = defaultThumbSize * horizontalScale;
            if (insetThumb)
            {
                resources["ScrollBarVerticalThumbMargin"] = new Thickness(3 * verticalScale, 3);
                resources["ScrollBarHorizontalThumbMargin"] = new Thickness(3, 3 * horizontalScale);
                resources["ScrollBarVerticalArrowSize"] = 8 * verticalScale;
                resources["ScrollBarHorizontalArrowSize"] = 8 * horizontalScale;
            }
        }
    }

    internal static double GetScale(object? value, string name)
    {
        if (value is null)
        {
            return 1;
        }

        if (value is string text &&
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int metric))
        {
            // Negative WindowMetrics values are twips; positive legacy values are pixels at 96 DPI.
            double pixels = metric < 0 ? -(double)metric / 15 : metric;
            if (pixels >= 8 && pixels <= 100)
            {
                return pixels / 17;
            }
        }

        Warn("Invalid Windows scrollbar preference {Preference}; retaining theme default.", name);
        return 1;
    }

    private static void Warn(string message, object value) =>
        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(typeof(WindowsScrollBarPreferences), message, value);
}
