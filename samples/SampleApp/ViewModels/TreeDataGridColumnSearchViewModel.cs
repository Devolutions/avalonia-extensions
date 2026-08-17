#if ENABLE_ACCELERATE
namespace SampleApp.ViewModels;

using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// Drives the column-search demo. Search terms live here rather than in the header so the demo shows the
/// adornment reacting to bound state, which is how a consumer is expected to wire it up.
/// </summary>
public partial class TreeDataGridColumnSearchViewModel : ObservableObject
{
    private const string LongTerm = "a-deliberately-long-search-term";

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

    private string nameSearch = string.Empty;

    private string typeSearch = string.Empty;

    private string statusSearch = string.Empty;

    public TreeDataGridColumnSearchViewModel()
    {
        this.VisibleNodes = new AvaloniaList<NetworkNode>(this.allNodes);
    }

    public AvaloniaList<NetworkNode> VisibleNodes { get; }

    public string NameSearch
    {
        get => this.nameSearch;
        set => this.SetSearch(ref this.nameSearch, value, nameof(this.NameSearch), nameof(this.HasNameSearch));
    }

    public string TypeSearch
    {
        get => this.typeSearch;
        set => this.SetSearch(ref this.typeSearch, value, nameof(this.TypeSearch), nameof(this.HasTypeSearch));
    }

    public string StatusSearch
    {
        get => this.statusSearch;
        set => this.SetSearch(ref this.statusSearch, value, nameof(this.StatusSearch), nameof(this.HasStatusSearch));
    }

    public bool HasNameSearch => !string.IsNullOrEmpty(this.nameSearch);

    public bool HasTypeSearch => !string.IsNullOrEmpty(this.typeSearch);

    public bool HasStatusSearch => !string.IsNullOrEmpty(this.statusSearch);

    public string MatchSummary => $"{this.VisibleNodes.Count} of {this.allNodes.Count} rows match";

    [RelayCommand]
    private void UseLongNameTerm() => this.NameSearch = LongTerm;

    [RelayCommand]
    private void ClearAll()
    {
        this.NameSearch = string.Empty;
        this.TypeSearch = string.Empty;
        this.StatusSearch = string.Empty;
    }

    private void SetSearch(ref string field, string value, string propertyName, string hasValuePropertyName)
    {
        if (!this.SetProperty(ref field, value ?? string.Empty, propertyName))
        {
            return;
        }

        this.OnPropertyChanged(hasValuePropertyName);
        this.ApplyFilter();
    }

    // All three terms must match, so searches accumulate across columns rather than replacing each other.
    private void ApplyFilter()
    {
        IEnumerable<NetworkNode> matches = this.allNodes
            .Where(node => Matches(node.Name, this.nameSearch))
            .Where(node => Matches(node.Type, this.typeSearch))
            .Where(node => Matches(node.Status, this.statusSearch));

        this.VisibleNodes.Clear();
        this.VisibleNodes.AddRange(matches);

        this.OnPropertyChanged(nameof(this.MatchSummary));
    }

    private static bool Matches(string? value, string term) =>
        string.IsNullOrEmpty(term) || (value ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase);
}
#endif
