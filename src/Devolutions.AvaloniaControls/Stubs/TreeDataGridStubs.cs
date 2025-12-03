#if !ENABLE_TREEDATAGRID
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Primitives")]

namespace Avalonia.Controls
{
    public class TreeDataGrid : TemplatedControl
    {
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var formattedText = new FormattedText(
                "TreeDataGrid is available in Avalonia Accelerate only.\n\nTo enable the commercial control, set the following environment variables:\n\n1. USE_AVALONIA_ACCELERATE_CONTROLS=true\n2. AVALONIA_LICENSE_KEY=<your_licence_key>",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                14,
                Brushes.Red
            );

            context.DrawText(formattedText, new Point(10, 10));
        }

        public static readonly StyledProperty<object?> SourceProperty =
            AvaloniaProperty.Register<TreeDataGrid, object?>(nameof(Source));
        public object? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

        public static readonly StyledProperty<bool> ShowColumnHeadersProperty =
            AvaloniaProperty.Register<TreeDataGrid, bool>(nameof(ShowColumnHeaders), true);
        public bool ShowColumnHeaders { get => GetValue(ShowColumnHeadersProperty); set => SetValue(ShowColumnHeadersProperty, value); }
        
        public static readonly StyledProperty<bool> CanUserSortColumnsProperty =
            AvaloniaProperty.Register<TreeDataGrid, bool>(nameof(CanUserSortColumns), true);
        public bool CanUserSortColumns { get => GetValue(CanUserSortColumnsProperty); set => SetValue(CanUserSortColumnsProperty, value); }
        
        public static readonly StyledProperty<bool> CanUserResizeColumnsProperty =
            AvaloniaProperty.Register<TreeDataGrid, bool>(nameof(CanUserResizeColumns), true);
        public bool CanUserResizeColumns { get => GetValue(CanUserResizeColumnsProperty); set => SetValue(CanUserResizeColumnsProperty, value); }

        public static readonly StyledProperty<object?> ElementFactoryProperty = AvaloniaProperty.Register<TreeDataGrid, object?>(nameof(ElementFactory));
        public object? ElementFactory { get => GetValue(ElementFactoryProperty); set => SetValue(ElementFactoryProperty, value); }

        public static readonly StyledProperty<object?> ColumnsProperty = AvaloniaProperty.Register<TreeDataGrid, object?>(nameof(Columns));
        public object? Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

        public static readonly StyledProperty<object?> RowsProperty = AvaloniaProperty.Register<TreeDataGrid, object?>(nameof(Rows));
        public object? Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }
    }

    public class TreeDataGridColumnHeadersPresenter : TemplatedControl
    {
        public static readonly StyledProperty<object?> ElementFactoryProperty = AvaloniaProperty.Register<TreeDataGridColumnHeadersPresenter, object?>(nameof(ElementFactory));
        public object? ElementFactory { get => GetValue(ElementFactoryProperty); set => SetValue(ElementFactoryProperty, value); }

        public static readonly StyledProperty<object?> ItemsProperty = AvaloniaProperty.Register<TreeDataGridColumnHeadersPresenter, object?>(nameof(Items));
        public object? Items { get => GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    }

    public class TreeDataGridRowsPresenter : TemplatedControl
    {
        public static readonly StyledProperty<object?> ElementFactoryProperty = AvaloniaProperty.Register<TreeDataGridRowsPresenter, object?>(nameof(ElementFactory));
        public object? ElementFactory { get => GetValue(ElementFactoryProperty); set => SetValue(ElementFactoryProperty, value); }

        public static readonly StyledProperty<object?> ItemsProperty = AvaloniaProperty.Register<TreeDataGridRowsPresenter, object?>(nameof(Items));
        public object? Items { get => GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

        public static readonly StyledProperty<object?> ColumnsProperty = AvaloniaProperty.Register<TreeDataGridRowsPresenter, object?>(nameof(Columns));
        public object? Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    }

    public class TreeDataGridColumnHeader : TemplatedControl
    {
        public static readonly StyledProperty<object?> SortDirectionProperty = AvaloniaProperty.Register<TreeDataGridColumnHeader, object?>(nameof(SortDirection));
        public object? SortDirection { get => GetValue(SortDirectionProperty); set => SetValue(SortDirectionProperty, value); }

        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty = AvaloniaProperty.Register<TreeDataGridColumnHeader, VerticalAlignment>(nameof(VerticalContentAlignment));
        public VerticalAlignment VerticalContentAlignment { get => GetValue(VerticalContentAlignmentProperty); set => SetValue(VerticalContentAlignmentProperty, value); }

        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty = AvaloniaProperty.Register<TreeDataGridColumnHeader, HorizontalAlignment>(nameof(HorizontalContentAlignment));
        public HorizontalAlignment HorizontalContentAlignment { get => GetValue(HorizontalContentAlignmentProperty); set => SetValue(HorizontalContentAlignmentProperty, value); }

        public static readonly StyledProperty<bool> CanUserResizeProperty = AvaloniaProperty.Register<TreeDataGridColumnHeader, bool>(nameof(CanUserResize));
        public bool CanUserResize { get => GetValue(CanUserResizeProperty); set => SetValue(CanUserResizeProperty, value); }

        public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<TreeDataGridColumnHeader, object?>(nameof(Header));
        public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

        public static readonly StyledProperty<object?> ContentTemplateProperty = AvaloniaProperty.Register<TreeDataGridColumnHeader, object?>(nameof(ContentTemplate));
        public object? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }
    }

    public class TreeDataGridRow : TemplatedControl
    {
        public static readonly StyledProperty<object?> ElementFactoryProperty = AvaloniaProperty.Register<TreeDataGridRow, object?>(nameof(ElementFactory));
        public object? ElementFactory { get => GetValue(ElementFactoryProperty); set => SetValue(ElementFactoryProperty, value); }

        public static readonly StyledProperty<object?> ColumnsProperty = AvaloniaProperty.Register<TreeDataGridRow, object?>(nameof(Columns));
        public object? Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

        public static readonly StyledProperty<object?> RowsProperty = AvaloniaProperty.Register<TreeDataGridRow, object?>(nameof(Rows));
        public object? Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }
    }

    public class TreeDataGridCellsPresenter : TemplatedControl
    {
        public static readonly StyledProperty<object?> ElementFactoryProperty = AvaloniaProperty.Register<TreeDataGridCellsPresenter, object?>(nameof(ElementFactory));
        public object? ElementFactory { get => GetValue(ElementFactoryProperty); set => SetValue(ElementFactoryProperty, value); }

        public static readonly StyledProperty<object?> ItemsProperty = AvaloniaProperty.Register<TreeDataGridCellsPresenter, object?>(nameof(Items));
        public object? Items { get => GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

        public static readonly StyledProperty<object?> RowsProperty = AvaloniaProperty.Register<TreeDataGridCellsPresenter, object?>(nameof(Rows));
        public object? Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }
    }

    public class TreeDataGridCheckBoxCell : TemplatedControl 
    {
        public static readonly StyledProperty<bool?> ValueProperty = AvaloniaProperty.Register<TreeDataGridCheckBoxCell, bool?>(nameof(Value));
        public bool? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public static readonly StyledProperty<bool> IsThreeStateProperty = AvaloniaProperty.Register<TreeDataGridCheckBoxCell, bool>(nameof(IsThreeState));
        public bool IsThreeState { get => GetValue(IsThreeStateProperty); set => SetValue(IsThreeStateProperty, value); }
    }

    public class TreeDataGridExpanderCell : TemplatedControl 
    {
        public static readonly StyledProperty<int> IndentProperty = AvaloniaProperty.Register<TreeDataGridExpanderCell, int>(nameof(Indent));
        public int Indent { get => GetValue(IndentProperty); set => SetValue(IndentProperty, value); }

        public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<TreeDataGridExpanderCell, bool>(nameof(IsExpanded));
        public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }

        public static readonly StyledProperty<bool> ShowExpanderProperty = AvaloniaProperty.Register<TreeDataGridExpanderCell, bool>(nameof(ShowExpander));
        public bool ShowExpander { get => GetValue(ShowExpanderProperty); set => SetValue(ShowExpanderProperty, value); }
    }

    public class TreeDataGridTextCell : TemplatedControl 
    {
        public static readonly StyledProperty<string?> ValueProperty = AvaloniaProperty.Register<TreeDataGridTextCell, string?>(nameof(Value));
        public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public static readonly StyledProperty<TextTrimming> TextTrimmingProperty = AvaloniaProperty.Register<TreeDataGridTextCell, TextTrimming>(nameof(TextTrimming));
        public TextTrimming TextTrimming { get => GetValue(TextTrimmingProperty); set => SetValue(TextTrimmingProperty, value); }

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty = AvaloniaProperty.Register<TreeDataGridTextCell, TextWrapping>(nameof(TextWrapping));
        public TextWrapping TextWrapping { get => GetValue(TextWrappingProperty); set => SetValue(TextWrappingProperty, value); }

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty = AvaloniaProperty.Register<TreeDataGridTextCell, TextAlignment>(nameof(TextAlignment));
        public TextAlignment TextAlignment { get => GetValue(TextAlignmentProperty); set => SetValue(TextAlignmentProperty, value); }
    }

    public class TreeDataGridTemplateCell : TemplatedControl 
    {
        public static readonly StyledProperty<object?> ContentTemplateProperty = AvaloniaProperty.Register<TreeDataGridTemplateCell, object?>(nameof(ContentTemplate));
        public object? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

        public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<TreeDataGridTemplateCell, object?>(nameof(Content));
        public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

        public static readonly StyledProperty<object?> EditingTemplateProperty = AvaloniaProperty.Register<TreeDataGridTemplateCell, object?>(nameof(EditingTemplate));
        public object? EditingTemplate { get => GetValue(EditingTemplateProperty); set => SetValue(EditingTemplateProperty, value); }
    }
}

namespace Avalonia.Controls.Converters
{
    public class IndentConverter : IValueConverter
    {
        public static readonly IndentConverter Instance = new IndentConverter();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
    }
}
#endif
