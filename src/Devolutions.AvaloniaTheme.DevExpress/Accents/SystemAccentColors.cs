// Retrieved from Avalonia Fluent at 27c1ece36cbe17de3b8f95ae88223ba68702ae47:
// https://github.com/AvaloniaUI/Avalonia/blob/27c1ece36cbe17de3b8f95ae88223ba68702ae47/src/Avalonia.Themes.Fluent/Accents/SystemAccentColors.cs
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Devolutions.AvaloniaTheme.DevExpress.Accents;

public sealed class SystemAccentColors : ResourceProvider
{
    public const string AccentKey = "SystemAccentColor";
    public const string AccentDark1Key = "SystemAccentColorDark1";
    public const string AccentDark2Key = "SystemAccentColorDark2";
    public const string AccentDark3Key = "SystemAccentColorDark3";
    public const string AccentLight1Key = "SystemAccentColorLight1";
    public const string AccentLight2Key = "SystemAccentColorLight2";
    public const string AccentLight3Key = "SystemAccentColorLight3";

    private static readonly Color s_defaultSystemAccentColor = Color.FromRgb(0, 120, 215);
    private bool _invalidateColors = true;
    private Color _systemAccentColor;
    private Color _systemAccentColorDark1, _systemAccentColorDark2, _systemAccentColorDark3;
    private Color _systemAccentColorLight1, _systemAccentColorLight2, _systemAccentColorLight3;

    public override bool HasResources => true;

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is string strKey)
        {
            EnsureColors();
            value = strKey switch
            {
                AccentKey => _systemAccentColor,
                AccentDark1Key => _systemAccentColorDark1,
                AccentDark2Key => _systemAccentColorDark2,
                AccentDark3Key => _systemAccentColorDark3,
                AccentLight1Key => _systemAccentColorLight1,
                AccentLight2Key => _systemAccentColorLight2,
                AccentLight3Key => _systemAccentColorLight3,
                _ => null
            };
            return value is not null;
        }

        value = null;
        return false;
    }

    protected override void OnAddOwner(IResourceHost owner)
    {
        if (GetFromOwner(owner) is { } platformSettings)
            platformSettings.ColorValuesChanged += PlatformSettingsOnColorValuesChanged;

        _invalidateColors = true;
    }

    protected override void OnRemoveOwner(IResourceHost owner)
    {
        if (GetFromOwner(owner) is { } platformSettings)
            platformSettings.ColorValuesChanged -= PlatformSettingsOnColorValuesChanged;

        _invalidateColors = true;
    }

    public static (Color d1, Color d2, Color d3, Color l1, Color l2, Color l3) CalculateAccentShades(Color accentColor)
    {
        const double dark1Step = 28.5 / 255d;
        const double dark2Step = 49 / 255d;
        const double dark3Step = 74.5 / 255d;
        const double light1Step = 39 / 255d;
        const double light2Step = 70 / 255d;
        const double light3Step = 103 / 255d;
        var hslAccent = accentColor.ToHsl();

        return (
            new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark1Step).ToRgb(),
            new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark2Step).ToRgb(),
            new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L - dark3Step).ToRgb(),
            new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light1Step).ToRgb(),
            new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light2Step).ToRgb(),
            new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + light3Step).ToRgb());
    }

    private void EnsureColors()
    {
        if (!_invalidateColors)
            return;

        _invalidateColors = false;
        _systemAccentColor = GetFromOwner(Owner)?.GetColorValues().AccentColor1 ?? s_defaultSystemAccentColor;
        (_systemAccentColorDark1, _systemAccentColorDark2, _systemAccentColorDark3,
            _systemAccentColorLight1, _systemAccentColorLight2, _systemAccentColorLight3) =
            CalculateAccentShades(_systemAccentColor);
    }

    private static IPlatformSettings? GetFromOwner(IResourceHost? owner) => owner switch
    {
        Application app => app.PlatformSettings,
        Visual visual => visual.GetPlatformSettings(),
        _ => null
    };

    private void PlatformSettingsOnColorValuesChanged(object? sender, PlatformColorValues e)
    {
        _invalidateColors = true;
        Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
    }
}
