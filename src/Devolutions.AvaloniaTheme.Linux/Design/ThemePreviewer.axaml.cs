namespace Devolutions.AvaloniaTheme.Linux.Design;

using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

[TemplatePart("PART_TabControl", typeof(TabControl), IsRequired = true)]
internal class ThemePreviewer : ContentControl
{
    private ContentPresenter? presenter;
    private TabControl? tabControl;
    private Border? currentContainer;
    private INameScope? nameScope;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (this.currentContainer is not null)
        {
            this.currentContainer.Child = null;
        }

        if (this.tabControl is not null)
        {
            this.tabControl.SelectionChanged -= this.TabControl_OnSelectionChanged;
        }

        base.OnApplyTemplate(e);

        this.nameScope = e.NameScope;
        this.tabControl = e.NameScope.Get<TabControl>("PART_TabControl");
        this.tabControl.SelectionChanged += this.TabControl_OnSelectionChanged;
    }

    private void TabControl_OnSelectionChanged(object? _, SelectionChangedEventArgs args)
    {
        if (args.RemovedItems.Count <= 0 || args.AddedItems.Count <= 0)
        {
            return;
        }

        TabItem? prevTab = args.RemovedItems[0] as TabItem;
        TabItem? nextTab = args.AddedItems[0] as TabItem;

        if (prevTab is null || nextTab is null || this.nameScope is null)
        {
            return;
        }

        this.presenter = this.nameScope.Get<ContentPresenter>("PART_ContentPresenter");

        Border? prevContainer = this.nameScope.Find<Border>($"PART_ContentContainer_{prevTab.Header}");
        Border? nextContainer = this.nameScope.Find<Border>($"PART_ContentContainer_{nextTab.Header}");

        if (prevContainer is not null && nextContainer is not null)
        {
            prevContainer.Child = null;
            nextContainer.Child = this.presenter;
            this.currentContainer = nextContainer;
        }
    }
}