using System;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using SampleApp.ViewModels;

namespace SampleApp.DemoPages;

public partial class TreeDataGridDemo : UserControl
{
  public TreeDataGridDemo()
  {
    InitializeComponent();
  }

  protected override void OnDataContextChanged(EventArgs e)
  {
      base.OnDataContextChanged(e);
      if (DataContext is TreeDataGridViewModel vm)
      {
          var template = this.FindResource("NameColumnTemplate") as IDataTemplate;
          if (template == null) return;

          // Add to CellSelectionSource
          if (vm.TreeCellSelectionSource != null && vm.TreeCellSelectionSource.Columns.Count > 0 && vm.TreeCellSelectionSource.Columns[0].Header as string != "Name")
          {
              var col = CreateNameExpanderColumn(template);
              vm.TreeCellSelectionSource.Columns.Insert(0, col);
          }

          // Add to RowSelectionSource
          if (vm.TreeRowSelectionSource != null && vm.TreeRowSelectionSource.Columns.Count > 0 && vm.TreeRowSelectionSource.Columns[0].Header as string != "Name")
          {
              var col = CreateNameExpanderColumn(template);
              vm.TreeRowSelectionSource.Columns.Insert(0, col);
          }
      }
  }

  private HierarchicalExpanderColumn<NetworkNode> CreateNameExpanderColumn(IDataTemplate template)
  {
      return new HierarchicalExpanderColumn<NetworkNode>(
          new TemplateColumn<NetworkNode>("Name", template),
          node => node.Children,
          hasChildrenSelector: node => node.Children.Count > 0,
          isExpandedSelector: node => node.IsExpanded
      );
  }
}