namespace Devolutions.AvaloniaControls.Tests;

using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Devolutions.AvaloniaControls.Styling;
using Devolutions.AvaloniaTheme.DevExpress;

/// <summary>
/// Merging <c>ManagedFileChooser.axaml</c> into a theme forced Avalonia.Dialogs to load while the theme was
/// being constructed — i.e. during application startup — for styles a host using the platform's own file
/// dialogs never shows. <see cref="DeferredResourceInclude"/> holds the dictionary back until something asks
/// for a key in the Avalonia.Dialogs namespace.
///
/// <para>
/// Note what is deliberately *not* asserted: whether Avalonia.Dialogs appears in
/// <c>AppDomain.CurrentDomain.GetAssemblies()</c>. Assembly loading is process-wide, and this test host
/// loads Actipro's ModernTheme, which pulls in Avalonia.Themes.Fluent, whose own ManagedFileChooser styles
/// load Avalonia.Dialogs before any test runs. Such an assertion would permanently skip and prove nothing,
/// so <see cref="DeferredResourceInclude.IsLoaded"/> observes our own deferral directly instead.
/// </para>
/// </summary>
public class DeferredManagedFileChooserTests
{
    private const string ChooserUri =
        "avares://Devolutions.AvaloniaTheme.DevExpress/Controls/ManagedFileChooser.axaml";

    /// <summary>Every theme that ships a copy of the managed file chooser dictionary.</summary>
    public static TheoryData<string> Themes => new(
        "Devolutions.AvaloniaTheme.DevExpress",
        "Devolutions.AvaloniaTheme.Linux",
        "Devolutions.AvaloniaTheme.MacOS");

    [Theory]
    [MemberData(nameof(Themes))]
    public void Theme_defers_the_managed_file_chooser_dictionary_instead_of_merging_it(string assembly)
    {
        string xaml = ReadSource($"{assembly}/Controls/FluentFallbackControls.axaml");

        Assert.DoesNotContain("MergeResourceInclude Source=\"ManagedFileChooser.axaml\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DeferredResourceInclude", xaml, StringComparison.Ordinal);
        Assert.Contains($"avares://{assembly}/Controls/ManagedFileChooser.axaml", xaml, StringComparison.Ordinal);

        // x:ClassModifier suppresses the standalone loader the XAML compiler emits, so a deferred load by
        // URI would fail at runtime with "No precompiled XAML found".
        Assert.DoesNotContain("x:ClassModifier=\"", ReadSource($"{assembly}/Controls/ManagedFileChooser.axaml"),
            StringComparison.Ordinal);
    }

    /// <summary>The mechanism in isolation: only a key in the trigger namespace may materialise it.</summary>
    [AvaloniaFact]
    public void Deferred_include_materialises_only_for_its_trigger_namespace()
    {
        var provider = new DeferredResourceInclude
        {
            Source = new Uri(ChooserUri),
            TriggerNamespace = "Avalonia.Dialogs",
        };

        Assert.False(provider.IsLoaded, "Nothing has been asked for yet.");

        Assert.False(provider.TryGetResource("SomeUnrelatedStringKey", ThemeVariant.Light, out _));
        Assert.False(provider.TryGetResource(typeof(Button), ThemeVariant.Light, out _));
        Assert.False(provider.IsLoaded, "Keys outside the trigger namespace must not materialise the dictionary.");

        Type chooser = ResolveDialogType("Avalonia.Dialogs.ManagedFileChooser");
        Assert.True(provider.TryGetResource(chooser, ThemeVariant.Light, out object? value));
        Assert.True(provider.IsLoaded, "A key in the trigger namespace should have materialised the dictionary.");

        var controlTheme = Assert.IsType<ControlTheme>(value);
        Assert.Equal(chooser, controlTheme.TargetType);
        Assert.NotEmpty(controlTheme.Setters);
    }

    /// <summary>
    /// The behavioural guard: building and applying the real theme must leave the dictionary unmaterialised,
    /// and resolving a dialogs key must then materialise it.
    /// </summary>
    [AvaloniaFact]
    public void Building_the_theme_leaves_the_dictionary_unmaterialised_until_a_dialog_key_is_requested()
    {
        Window window = ShowWithFullTheme(ThemeVariant.Light);

        DeferredResourceInclude provider = Assert.Single(FindDeferred(window.Styles).ToArray());

        Assert.False(provider.IsLoaded,
            "Constructing DevolutionsDevExpressTheme populated the managed file chooser dictionary. It is " +
            "being merged eagerly again, which loads Avalonia.Dialogs during application startup.");

        Assert.True(window.TryFindResource(ResolveDialogType("Avalonia.Dialogs.ManagedFileChooser"),
            ThemeVariant.Light, out _));

        Assert.True(provider.IsLoaded, "Resolving a dialogs key should have materialised the dictionary.");
    }

    /// <summary>Both ControlThemes the dictionary contributes must survive the move off the merge path.</summary>
    [AvaloniaTheory]
    [InlineData("Avalonia.Dialogs.ManagedFileChooser")]
    [InlineData("Avalonia.Dialogs.ManagedFileChooserOverwritePrompt")]
    public void Managed_file_chooser_themes_still_resolve_on_demand(string typeName)
    {
        Window window = ShowWithFullTheme(ThemeVariant.Light);
        Type target = ResolveDialogType(typeName);

        Assert.True(window.TryFindResource(target, ThemeVariant.Light, out object? value),
            $"No ControlTheme resolved for {typeName}; the deferred provider did not materialise.");

        var controlTheme = Assert.IsType<ControlTheme>(value);
        Assert.Equal(target, controlTheme.TargetType);
        Assert.NotEmpty(controlTheme.Setters);
    }

    /// <summary>Resolved by name so the test does not reference Avalonia.Dialogs until it means to.</summary>
    private static Type ResolveDialogType(string typeName) =>
        Type.GetType($"{typeName}, Avalonia.Dialogs")
        ?? throw new InvalidOperationException($"{typeName} not found; is Avalonia.Dialogs deployed?");

    private static string ReadSource(string relative) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../../src/", relative));

    private static IEnumerable<DeferredResourceInclude> FindDeferred(object? node)
    {
        switch (node)
        {
            case DeferredResourceInclude deferred:
                yield return deferred;
                break;

            case IResourceDictionary dictionary:
                foreach (IResourceProvider merged in dictionary.MergedDictionaries)
                {
                    foreach (DeferredResourceInclude found in FindDeferred(merged))
                    {
                        yield return found;
                    }
                }

                break;

            case Styles styles:
                foreach (DeferredResourceInclude found in FindDeferred(styles.Resources))
                {
                    yield return found;
                }

                foreach (IStyle child in styles)
                {
                    foreach (DeferredResourceInclude found in FindDeferred(child))
                    {
                        yield return found;
                    }
                }

                break;

            case StyleInclude include:
                foreach (DeferredResourceInclude found in FindDeferred(include.Loaded))
                {
                    yield return found;
                }

                break;
        }
    }

    private static Window ShowWithFullTheme(ThemeVariant variant)
    {
        var theme = new DevolutionsDevExpressTheme();
        theme.BeginInit();
        theme.EndInit();

        var window = new Window { RequestedThemeVariant = variant };
        window.Styles.Add(theme);
        window.Show();
        return window;
    }
}
