#if ENABLE_ACCELERATE
namespace SampleApp.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// Search state for a single column, cycling between three header states: an idle column showing only a
/// magnifier on hover, an editing column showing a field spanning the header, and a searched column
/// showing the committed term.
/// </summary>
public partial class ColumnSearchViewModel : ObservableObject
{
    private readonly Action onCommittedChanged;

    private string term = string.Empty;

    private string draft = string.Empty;

    private bool isEditing;

    public ColumnSearchViewModel(string columnName, Action onCommittedChanged)
    {
        this.ColumnName = columnName;
        this.onCommittedChanged = onCommittedChanged;
    }

    public string ColumnName { get; }

    public string Watermark => $"Search {this.ColumnName.ToLowerInvariant()}";

    /// <summary>Committed term, i.e. the one actually filtering rows.</summary>
    public string Term
    {
        get => this.term;
        private set
        {
            if (this.SetProperty(ref this.term, value))
            {
                this.OnPropertyChanged(nameof(this.HasTerm));
                this.onCommittedChanged();
            }
        }
    }

    /// <summary>What the user is typing; discarded unless committed.</summary>
    public string Draft
    {
        get => this.draft;
        set => this.SetProperty(ref this.draft, value ?? string.Empty);
    }

    public bool IsEditing
    {
        get => this.isEditing;
        private set => this.SetProperty(ref this.isEditing, value);
    }

    public bool HasTerm => !string.IsNullOrEmpty(this.term);

    [RelayCommand]
    private void BeginEdit()
    {
        this.Draft = this.term;
        this.IsEditing = true;
    }

    [RelayCommand]
    private void Commit()
    {
        this.Term = this.draft;
        this.IsEditing = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        this.Draft = this.term;
        this.IsEditing = false;
    }

    /// <summary>Clears the term and leaves edit mode, as the X button does.</summary>
    [RelayCommand]
    private void Clear()
    {
        this.Draft = string.Empty;
        this.Term = string.Empty;
        this.IsEditing = false;
    }
}
#endif
