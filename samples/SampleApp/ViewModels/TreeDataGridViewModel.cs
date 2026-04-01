#if ENABLE_ACCELERATE
using Avalonia.Controls;
using Avalonia.Controls.Selection;
#endif

namespace SampleApp.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

public class NetworkNode
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Folder", "Computer", "User"
    public string IPAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LastSeen { get; set; } = string.Empty;

    public string IconPath => Type switch
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
        this.IPAddress = ip;
        this.Status = status;
        this.LastSeen = lastSeen;
    }
}

public class TreeDataGridViewModel : ObservableObject
{
#if ENABLE_ACCELERATE
    private readonly ObservableCollection<NetworkNode> flatNodes =
    [
        new NetworkNode("DC01", "Computer", "192.168.1.10", "Online", "Just now"),
        new NetworkNode("WEB01", "Computer", "192.168.1.20", "Online", "5 mins ago"),
        new NetworkNode("SQL01", "Computer", "192.168.1.30", "Warning", "1 hour ago"),

        new NetworkNode("DESKTOP-A", "Computer", "10.0.0.5", "Offline", "2 days ago"),
        new NetworkNode("DESKTOP-B", "Computer", "10.0.0.6", "Online", "Just now"),

        new NetworkNode("Admin", "User", "", "Active", "Just now"),
        new NetworkNode("Guest", "User", "", "Inactive", "Never"),
    ];

    private readonly ObservableCollection<NetworkNode> treeNodes =
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
                }
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
        source.WithTextColumn("Type", x => x.Type);
        source.WithTextColumn("IP Address", x => x.IPAddress);
        source.WithTextColumn("Status", x => x.Status);
        source.WithTextColumn("Last Seen", x => x.LastSeen);
    }

    private static void AddTreeSharedColumns(HierarchicalTreeDataGridSource<NetworkNode> source)
    {
        // Name column will be added by the View to use XAML DataTemplate
        source.WithTextColumn("Type", x => x.Type);
        source.WithTextColumn("IP Address", x => x.IPAddress);
        source.WithTextColumn("Status", x => x.Status);
        source.WithTextColumn("Last Seen", x => x.LastSeen);
    }

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