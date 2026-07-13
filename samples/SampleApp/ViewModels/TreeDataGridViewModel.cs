#if ENABLE_ACCELERATE
using Avalonia.Controls;
using Avalonia.Controls.Selection;
#endif

namespace SampleApp.ViewModels;

using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Svg;
using CommunityToolkit.Mvvm.ComponentModel;
using Devolutions.AvaloniaControls.Controls;

public class NetworkNode
{
    public string Name { get; set; }
    public string Type { get; set; } // "Folder", "Computer", "User"
    public bool IsExpanded { get; set; }
    public string IPAddress { get; set; }
    public string Status { get; set; }
    public string LastSeen { get; set; }
    public bool HasCheckBox => this.Type == "Computer";
    public bool IsChecked { get; set; }
    public bool HasChildren => this.Children.Count > 0;

    public string IconPath => this.Type switch
    {
        "Folder" => "/Assets/Folder.svg",
        "Computer" => "/Assets/Computer.svg",
        "User" => "/Assets/User.svg",
        _ => "/Assets/Help.svg",
    };

    public ObservableCollection<NetworkNode> Children { get; } = new();

    public NetworkNode(string name, string type, string ip = "", string status = "", string lastSeen = "")
    {
        this.Name = name;
        this.Type = type;
        this.IsExpanded = type == "Folder" && name != "Workstations";
        this.IPAddress = ip;
        this.Status = status;
        this.LastSeen = lastSeen;
        this.IsChecked = status == "Online";
    }
}

public sealed record IconColumnHeader(string IconPath, string ToolTip);

public class TreeDataGridViewModel : ObservableObject
{
#if ENABLE_ACCELERATE
    private readonly AvaloniaList<NetworkNode> flatNodes =
    [
        new NetworkNode("DC01", "Computer", "192.168.1.10", "Online", "Just now"),
        new NetworkNode("WEB01", "Computer", "192.168.1.20", "Online", "5 mins ago"),
        new NetworkNode("SQL01", "Computer", "192.168.1.30", "Warning", "1 hour ago"),

        new NetworkNode("DESKTOP-A", "Computer", "10.0.0.5", "Offline", "2 days ago"),
        new NetworkNode("DESKTOP-B", "Computer", "10.0.0.6", "Online", "Just now"),

        new NetworkNode("Admin", "User", "", "Active", "Just now"),
        new NetworkNode("Guest", "User", "", "Inactive", "Never"),
    ];

    private readonly AvaloniaList<NetworkNode> treeNodes =
    [
        new NetworkNode("Data Center", "Folder")
        {
            Children =
            {
                new NetworkNode("Servers", "Folder")
                {
                    Children =
                    {
                        new NetworkNode("DC01", "Computer", "192.168.1.10", "Online", "Just now"),
                        new NetworkNode("WEB01", "Computer", "192.168.1.20", "Online", "5 mins ago"),
                        new NetworkNode("SQL01", "Computer", "192.168.1.30", "Warning", "1 hour ago"),
                    }
                },
                new NetworkNode("Workstations", "Folder")
                {
                    Children =
                    {
                        new NetworkNode("DESKTOP-A", "Computer", "10.0.0.5", "Offline", "2 days ago"),
                        new NetworkNode("DESKTOP-B", "Computer", "10.0.0.6", "Online", "Just now"),
                    }
                },
                new NetworkNode("OddOneOut", "Unknown", "192.168.1.150", "Online", "Just now")
            }
        },

        new NetworkNode("Users", "Folder")
        {
            Children =
            {
                new NetworkNode("Admin", "User", "", "Active", "Just now"),
                new NetworkNode("Guest", "User", "", "Inactive", "Never"),
            }
        },
    ];

    public TreeDataGridViewModel()
    {
        for (int i = 2; i < 1000; ++i)
        {
            this.flatNodes.Add(new NetworkNode($"Guest {i}", "User", "", "Inactive", "Never"));
        }

        this.CellSelectionSource = new FlatTreeDataGridSource<NetworkNode>(this.flatNodes);
        AddSharedColumns(this.CellSelectionSource);
        this.CellSelectionSource.Selection = new TreeDataGridCellSelectionModel<NetworkNode>(this.CellSelectionSource);

        this.RowSelectionSource = new FlatTreeDataGridSource<NetworkNode>(this.flatNodes);
        AddSharedColumns(this.RowSelectionSource);
        this.RowSelectionSource.Columns[2].Width = new GridLength(90);
        this.RowSelectionSource.Columns[3].Width = new GridLength(90);
        this.RowSelectionSource.Selection = new TreeDataGridRowSelectionModel<NetworkNode>(this.RowSelectionSource);

        this.TreeCellSelectionSource = new HierarchicalTreeDataGridSource<NetworkNode>(this.treeNodes);
        AddTreeSharedColumns(this.TreeCellSelectionSource);
        this.TreeCellSelectionSource.Selection = new TreeDataGridCellSelectionModel<NetworkNode>(this.TreeCellSelectionSource);

        this.TreeRowSelectionSource = new HierarchicalTreeDataGridSource<NetworkNode>(this.treeNodes);
        AddTreeSharedColumns(this.TreeRowSelectionSource);
        this.TreeRowSelectionSource.Selection = new TreeDataGridRowSelectionModel<NetworkNode>(this.TreeRowSelectionSource);
    }

    private static void AddSharedColumns(FlatTreeDataGridSource<NetworkNode> source)
    {
        source.WithTextColumn("Name", x => x.Name);
        AddTextColumnWithToolTip(source, "Type", x => x.Type, "Explicit consumer tooltip: Type header");
        source.WithTextColumn(
            "IP Address (IPv4 or IPv6)",
            x => x.IPAddress,
            options =>
            {
                options.TextTrimming = TextTrimming.CharacterEllipsis;
            });
        TreeDataGridOverflowHeader.SetColumnToolTip(source.Columns[^1], "Explicit consumer tooltip: IP Address header");
        source.WithTextColumn("Status of the item", x => x.Status);
        source.WithTextColumn("Last Seen", x => x.LastSeen);
        source.WithTemplateColumnFromResourceKeys("Icon", "IconColumnTemplate");
        source.WithTemplateColumnFromResourceKeys(
            new IconColumnHeader("/Assets/Computer.svg", "Icon header template + checkbox cells"),
            "CheckBoxColumnTemplate");
        TreeDataGridOverflowHeader.SetColumnToolTip(source.Columns[^1], "Icon header template + checkbox cells");
    }

    private static void AddTreeSharedColumns(HierarchicalTreeDataGridSource<NetworkNode> source)
    {
        // Name column will be added by the View to use XAML DataTemplate
        source.WithTextColumn("Type", x => x.Type);
        source.WithTextColumn("IP Address", x => x.IPAddress);
        source.WithTextColumn("Status", x => x.Status);
        source.WithTextColumn("Last Seen", x => x.LastSeen);
    }

    private static void AddTextColumnWithToolTip<TValue>(
        FlatTreeDataGridSource<NetworkNode> source,
        string headerText,
        System.Linq.Expressions.Expression<Func<NetworkNode, TValue>> expression,
        string tooltipText)
    {
        source.WithTextColumn(headerText, expression);
        TreeDataGridOverflowHeader.SetColumnToolTip(source.Columns[^1], tooltipText);
    }

    public AvaloniaList<NetworkNode> FlatNodes => this.flatNodes;
    
    public FlatTreeDataGridSource<NetworkNode> CellSelectionSource { get; }
    public FlatTreeDataGridSource<NetworkNode> RowSelectionSource { get; }

    public HierarchicalTreeDataGridSource<NetworkNode> TreeCellSelectionSource { get; }
    public HierarchicalTreeDataGridSource<NetworkNode> TreeRowSelectionSource { get; }
#else
  public object? CellSelectionSource { get; } = null;
  public object? RowSelectionSource { get; } = null;

  public object? TreeCellSelectionSource { get; } = null;
  public object? TreeRowSelectionSource { get; } = null;
#endif
}
