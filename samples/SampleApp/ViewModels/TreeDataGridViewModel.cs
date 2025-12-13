#if ENABLE_ACCELERATE
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Svg;
using System;
#endif

namespace SampleApp.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

public class NetworkNode
{
  public string Name { get; set; }
  public string Type { get; set; } // "Folder", "Computer", "User"
  public string IPAddress { get; set; }
  public string Status { get; set; }
  public string LastSeen { get; set; }
  public string IconPath => Type switch
  {
      "Folder" => "/Assets/Folder.svg",
      "Computer" => "/Assets/Computer.svg",
      "User" => "/Assets/User.svg",
      _ => "/Assets/Help.svg"
  };
  public ObservableCollection<NetworkNode> Children { get; } = new();

  public NetworkNode(string name, string type, string ip = "", string status = "", string lastSeen = "")
  {
    Name = name;
    Type = type;
    IPAddress = ip;
    Status = status;
    LastSeen = lastSeen;
  }
}

public class TreeDataGridViewModel : ObservableObject
{
#if ENABLE_ACCELERATE
  private readonly ObservableCollection<NetworkNode> _nodes = new()
  {
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
            new NetworkNode("SQL01", "Computer", "192.168.1.30", "Warning", "1 hour ago")
          }
        },
        new NetworkNode("Workstations", "Folder")
        {
          Children =
          {
            new NetworkNode("DESKTOP-A", "Computer", "10.0.0.5", "Offline", "2 days ago"),
            new NetworkNode("DESKTOP-B", "Computer", "10.0.0.6", "Online", "Just now")
          }
        }
      }
    },
    new NetworkNode("Users", "Folder")
    {
      Children =
      {
        new NetworkNode("Admin", "User", "", "Active", "Just now"),
        new NetworkNode("Guest", "User", "", "Inactive", "Never")
      }
    }
  };

  public TreeDataGridViewModel()
  {
    CellSelectionSource = new HierarchicalTreeDataGridSource<NetworkNode>(_nodes)
    {
      Columns =
      {
        // Name column will be added by the View to use XAML DataTemplate
        new TextColumn<NetworkNode, string>("Type", x => x.Type),
        new TextColumn<NetworkNode, string>("IP Address", x => x.IPAddress),
        new TextColumn<NetworkNode, string>("Status", x => x.Status),
        new TextColumn<NetworkNode, string>("Last Seen", x => x.LastSeen)
      }
    };
    CellSelectionSource.Selection = new TreeDataGridCellSelectionModel<NetworkNode>(CellSelectionSource);

    RowSelectionSource = new HierarchicalTreeDataGridSource<NetworkNode>(_nodes)
    {
      Columns =
      {
        // Name column will be added by the View to use XAML DataTemplate
        new TextColumn<NetworkNode, string>("Type", x => x.Type),
        new TextColumn<NetworkNode, string>("IP Address", x => x.IPAddress),
        new TextColumn<NetworkNode, string>("Status", x => x.Status),
        new TextColumn<NetworkNode, string>("Last Seen", x => x.LastSeen)
      }
    };
  }

  public HierarchicalTreeDataGridSource<NetworkNode> CellSelectionSource { get; }
  public HierarchicalTreeDataGridSource<NetworkNode> RowSelectionSource { get; }
#else
  public object? CellSelectionSource { get; } = null;
  public object? RowSelectionSource { get; } = null;
#endif
}