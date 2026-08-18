#if ENABLE_ACCELERATE
namespace SampleApp.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// Hosts one <see cref="ColumnSearchViewModel"/> per searchable column. Terms accumulate: a row has to
/// match every committed term.
/// </summary>
public partial class TreeDataGridColumnSearchViewModel : ObservableObject
{
    private readonly List<NetworkNode> allNodes =
    [
        new NetworkNode("DC01", "Computer", "192.168.1.10", "Online", "Just now"),
        new NetworkNode("WEB01", "Computer", "192.168.1.20", "Online", "5 mins ago"),
        new NetworkNode("SQL01", "Computer", "192.168.1.30", "Warning", "1 hour ago"),
        new NetworkNode("DESKTOP-A", "Computer", "10.0.0.5", "Offline", "2 days ago"),
        new NetworkNode("DESKTOP-B", "Computer", "10.0.0.6", "Online", "Just now"),
        new NetworkNode("Admin", "User", "", "Active", "Just now"),
        new NetworkNode("Guest", "User", "", "Inactive", "Never"),
    ];

    public TreeDataGridColumnSearchViewModel()
    {
        this.NameSearch = new ColumnSearchViewModel("Name", this.ApplyFilter);
        this.TypeSearch = new ColumnSearchViewModel("Type", this.ApplyFilter);
        this.StatusSearch = new ColumnSearchViewModel("Status", this.ApplyFilter);

        this.VisibleNodes = new AvaloniaList<NetworkNode>(this.allNodes);
    }

    public ColumnSearchViewModel NameSearch { get; }

    public ColumnSearchViewModel TypeSearch { get; }

    public ColumnSearchViewModel StatusSearch { get; }

    public AvaloniaList<NetworkNode> VisibleNodes { get; }

    public string MatchSummary => $"{this.VisibleNodes.Count} of {this.allNodes.Count} rows match";

    public string ActiveTerms
    {
        get
        {
            string[] active = this.Searches
                .Where(static search => search.HasTerm)
                .Select(static search => $"{search.ColumnName}=\"{search.Term}\"")
                .ToArray();

            return active.Length == 0 ? "no column search applied" : string.Join(" AND ", active);
        }
    }

    private IEnumerable<ColumnSearchViewModel> Searches
    {
        get
        {
            yield return this.NameSearch;
            yield return this.TypeSearch;
            yield return this.StatusSearch;
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        foreach (ColumnSearchViewModel search in this.Searches)
        {
            search.ClearCommand.Execute(null);
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<NetworkNode> matches = this.allNodes
            .Where(node => Matches(node.Name, this.NameSearch.Term))
            .Where(node => Matches(node.Type, this.TypeSearch.Term))
            .Where(node => Matches(node.Status, this.StatusSearch.Term));

        this.VisibleNodes.Clear();
        this.VisibleNodes.AddRange(matches);

        this.OnPropertyChanged(nameof(this.MatchSummary));
        this.OnPropertyChanged(nameof(this.ActiveTerms));
    }

    private static bool Matches(string? value, string term) =>
        string.IsNullOrEmpty(term) || (value ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase);
}
#endif
