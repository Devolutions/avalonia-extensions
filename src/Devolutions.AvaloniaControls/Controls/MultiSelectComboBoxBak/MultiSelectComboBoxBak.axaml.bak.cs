// namespace Devolutions.AvaloniaControls.Controls;
//
// using System.Collections;
// using Avalonia;
// using Avalonia.Collections;
// using Avalonia.Controls;
// using Avalonia.Controls.Metadata;
// using Avalonia.Controls.Primitives;
//
// [TemplatePart("PART_Popup", typeof(Popup), IsRequired = true)]
// [TemplatePart("PART_CheckBoxListBox", typeof(CheckBoxListBox), IsRequired = true)]
// public class MultiSelectComboBox : ItemsControl
// {
//     public static readonly StyledProperty<bool> IsDropDownOpenProperty =
//         AvaloniaProperty.Register<MultiSelectComboBox, bool>(nameof(IsDropDownOpen));
//
//     public static readonly StyledProperty<Thickness> FocusedBorderThicknessProperty = AvaloniaProperty.Register<EditableComboBox, Thickness>(
//         nameof(FocusedBorderThickness),
//         Application.Current?.FindResource("TextControlBorderThemeThicknessFocused") as Thickness? ?? new Thickness(2));
//
//     public static readonly DirectProperty<MultiSelectComboBox, IEnumerable> InnerLeftContentProperty =
//         AvaloniaProperty.RegisterDirect<MultiSelectComboBox, IEnumerable>(
//             nameof(InnerLeftContent),
//             static o => o.InnerLeftContent,
//             static (o, v) => o.InnerLeftContent = v);
//
//     public static readonly DirectProperty<MultiSelectComboBox, IEnumerable> InnerRightContentProperty =
//         AvaloniaProperty.RegisterDirect<MultiSelectComboBox, IEnumerable>(
//             nameof(InnerRightContent),
//             static o => o.InnerRightContent,
//             static (o, v) => o.InnerRightContent = v);
//
//     private CheckBoxListBox? list;
//     private Popup? popup;
//
//     public MultiSelectComboBox()
//     {
//         this.Items.CollectionChanged += (_, args) =>
//         {
//             if (this.list is null) return;
//
//             if (args.NewItems is not null)
//             {
//                 foreach (var item in args.NewItems)
//                 {
//                     this.list.Items.Add(item);
//                 }
//             }
//         };
//     }
//
//     public IEnumerable InnerLeftContent { get; set; } = new AvaloniaList<Control>();
//
//     public IEnumerable InnerRightContent { get; set; } = new AvaloniaList<Control>();
//
//     public bool IsDropDownOpen
//     {
//         get => this.GetValue(IsDropDownOpenProperty);
//         set => this.SetValue(IsDropDownOpenProperty, value);
//     }
//
//     public Thickness FocusedBorderThickness
//     {
//         get => this.GetValue(FocusedBorderThicknessProperty);
//         set => this.SetValue(FocusedBorderThicknessProperty, value);
//     }
//
//     protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
//     {
//         base.OnApplyTemplate(e);
//
//         this.popup = e.NameScope.Get<Popup>("PART_Popup");
//         this.list = e.NameScope.Get<CheckBoxListBox>("PART_CheckBoxListBox");
//
//         foreach (object? item in this.Items)
//         {
//             this.list.Items.Add(item);
//         }
//     }
// }

