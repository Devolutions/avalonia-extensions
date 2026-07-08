using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using SampleApp.ViewModels;

namespace SampleApp.DemoPages;

public partial class TreeDataGridDemo : UserControl
{
  public TreeDataGridDemo()
  {
      this.InitializeComponent();
  }

  protected override void OnDataContextChanged(EventArgs e)
  {
      base.OnDataContextChanged(e);
      if (this.DataContext is TreeDataGridViewModel vm)
      {
          var template = this.FindResource("NameColumnTemplate") as IDataTemplate;
          if (template == null) return;

          // Add to CellSelectionSource
          if (vm.TreeCellSelectionSource.Columns.Count > 0 && vm.TreeCellSelectionSource.Columns[0].Header as string != "Name")
          {
              var col = this.CreateNameExpanderColumn(template);
              vm.TreeCellSelectionSource.Columns.Insert(0, col);
          }

          // Add to RowSelectionSource
          if (vm.TreeRowSelectionSource.Columns.Count > 0 && vm.TreeRowSelectionSource.Columns[0].Header as string != "Name")
          {
              var col = this.CreateNameExpanderColumn(template);
              vm.TreeRowSelectionSource.Columns.Insert(0, col);
          }
      }
  }

  private TreeDataGridHierarchicalExpanderColumn CreateNameExpanderColumn(IDataTemplate template)
  {
      return new TreeDataGridHierarchicalExpanderColumn()
      {
          Header = "Name",
          Inner = new TreeDataGridTemplateColumn()
          {
              CellTemplate = template,
          },
          ChildrenBinding = CompiledBinding.Create((NetworkNode node) => node.Children),
          HasChildrenBinding = CompiledBinding.Create((NetworkNode node) => node.HasChildren),
          IsExpandedBinding = CompiledBinding.Create((NetworkNode node) => node.IsExpanded),
      };
  }
}