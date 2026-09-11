// Retrieved from Avalonia Fluent at cd3b218eb4f652905bfdab7a0e704a6139c5d332:
// https://github.com/AvaloniaUI/Avalonia/blob/cd3b218eb4f652905bfdab7a0e704a6139c5d332/src/Avalonia.Themes.Fluent/ColorPaletteResources.cs
// Adapted as an immutable Linux Yaru fixed-accent provider.
using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Devolutions.AvaloniaTheme.Linux.Accents;

public sealed class YaruAccentColors : ResourceProvider
{
    private static readonly Color s_accentColor = Color.Parse("#D85E33");
    private static readonly (Color Dark1, Color Dark2, Color Dark3, Color Light1, Color Light2, Color Light3) s_shades =
        SystemAccentColors.CalculateAccentShades(s_accentColor);

    public override bool HasResources => true;

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        value = (key as string) switch
        {
            SystemAccentColors.AccentKey => s_accentColor,
            SystemAccentColors.AccentDark1Key => s_shades.Dark1,
            SystemAccentColors.AccentDark2Key => s_shades.Dark2,
            SystemAccentColors.AccentDark3Key => s_shades.Dark3,
            SystemAccentColors.AccentLight1Key => s_shades.Light1,
            SystemAccentColors.AccentLight2Key => s_shades.Light2,
            SystemAccentColors.AccentLight3Key => s_shades.Light3,
            _ => null
        };

        return value is not null;
    }
}
