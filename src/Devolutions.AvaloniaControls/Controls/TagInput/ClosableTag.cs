// Vendored from Ursa.Avalonia, MIT licensed.
//   source:  https://github.com/irihitech/Ursa.Avalonia/blob/e72d50d5e41efa75424c3d2b39f4131a2293b155/src/Ursa/Controls/TagInput/ClosableTag.cs
//   commit:  e72d50d5e41efa75424c3d2b39f4131a2293b155  (shipped as Irihi.Ursa 2.0.1)
//
// We used four types out of Ursa's 491 and wrapped two of them in empty
// subclasses purely to re-expose them under our own namespace. Vendoring drops
// both the Irihi.Ursa and Irihi.Avalonia.Shared package references, removes those
// wrappers, and stops Ursa loading during theme construction at application start.
//
// Changes from upstream:
//   - namespace moved to Devolutions.AvaloniaControls.Controls
//   - accessibility: an automation peer named by the tag's text, and RemoveText to name the close button
// See THIRD-PARTY-NOTICES.md for the full licence text.
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Devolutions.AvaloniaControls.Controls;

[TemplatePart(PART_CloseButton, typeof(Button))]
public class ClosableTag : ContentControl
{
    public const string PART_CloseButton = "PART_CloseButton";

    public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<ClosableTag, ICommand?>(
        nameof(Command));

    public static readonly StyledProperty<string> RemoveTextProperty = AvaloniaProperty.Register<ClosableTag, string>(
        nameof(RemoveText), "Remove {0}");

    private Button? closeButton;

    private TextBlock[] watchedText = [];

    internal Button? CloseButton => closeButton;

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The close button's accessible name; {0} is the tag's text.</summary>
    public string RemoveText
    {
        get => GetValue(RemoveTextProperty);
        set => SetValue(RemoveTextProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ClosableTagAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        closeButton = e.NameScope.Find<Button>(PART_CloseButton);
        NameCloseButtonAfterLayout();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty || change.Property == DataContextProperty || change.Property == RemoveTextProperty
            || change.Property == AutomationProperties.NameProperty)
        {
            NameCloseButtonAfterLayout();
        }
    }

    // The tag's text can come from its content template, which only exists once laid out.
    private void NameCloseButtonAfterLayout() => Dispatcher.UIThread.Post(NameCloseButton, DispatcherPriority.Loaded);

    private void NameCloseButton()
    {
        if (closeButton is null)
        {
            return;
        }

        AutomationProperties.SetName(closeButton, string.Format(CultureInfo.CurrentCulture, RemoveText, AccessibleContentText.Get(this)));

        // A bound label can change without the content changing.
        foreach (TextBlock block in watchedText)
        {
            block.PropertyChanged -= OnShownTextChanged;
        }

        watchedText = [.. this.GetVisualDescendants().OfType<TextBlock>()];
        foreach (TextBlock block in watchedText)
        {
            block.PropertyChanged += OnShownTextChanged;
        }
    }

    private void OnShownTextChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.TextProperty || e.Property == TextBlock.InlinesProperty)
        {
            NameCloseButtonAfterLayout();
        }
    }
}
