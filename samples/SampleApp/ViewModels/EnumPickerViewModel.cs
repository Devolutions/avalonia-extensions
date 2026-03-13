namespace SampleApp.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public enum DemoStatus
{
    Active,
    Inactive,
    Pending,
    Archived,
}

public enum DemoPriority
{
    Low,
    Normal,
    High,
    Critical,
    Blocker
}

public partial class EnumPickerViewModel : ObservableObject
{
    [ObservableProperty]
    private DemoPriority? selectedDefault;

    [ObservableProperty]
    private DemoPriority? selectedSorted;

    [ObservableProperty]
    private DemoPriority? selectedExcluded;

    [ObservableProperty]
    private DemoPriority? selectedIncluded;

    [ObservableProperty]
    private DemoPriority? selectedWithOverrides;

    [ObservableProperty]
    private DemoPriority? selectedWithProvider;

    [ObservableProperty]
    private DemoStatus? selectedStatus;

    [ObservableProperty]
    private DemoPriority? selectedInlineExcluded;

    [ObservableProperty]
    private DemoPriority? selectedInlineIncluded;

    public Func<object, string> TextProvider { get; } = priority => priority switch
    {
        DemoPriority.Low => "Low ↓",
        DemoPriority.Normal => "Normal →",
        DemoPriority.High => "High ↑",
        DemoPriority.Critical => "Critical !!",
        DemoPriority.Blocker => "Blocker ✗",
        _ => priority.ToString() ?? string.Empty
    };

    public IReadOnlyDictionary<DemoPriority, string> TextOverrides { get; } = new Dictionary<DemoPriority, string>
    {
        { DemoPriority.High, "High (overridden)" },
        { DemoPriority.Critical, "CRITICAL" }
    };

    public IReadOnlyCollection<DemoPriority> ExcludedValues { get; } = [DemoPriority.Blocker];

    public IReadOnlyCollection<DemoPriority> IncludedValues { get; } = [DemoPriority.Low, DemoPriority.Normal, DemoPriority.High];
}