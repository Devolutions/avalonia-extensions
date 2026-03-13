namespace SampleApp.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public enum DemoStatus
{
    Active,
    Inactive,
    Pending,
    Archived,
}

// Declared alphabetically (as is common in codebases), but displayed in workflow order via CustomSort
public enum DemoTaskStatus
{
    Blocked,
    Cancelled,
    Done,
    InProgress,
    Todo,
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

    [ObservableProperty]
    private DemoPriority? selectedInlineOverrides;

    [ObservableProperty]
    private DemoTaskStatus? selectedCustomSort;

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

    // Enum is alphabetical in code, but displayed in workflow order
    public Comparison<DemoTaskStatus> CustomSort { get; } = (a, b) =>
    {
        static int WorkflowIndex(DemoTaskStatus s) => s switch
        {
            DemoTaskStatus.Todo       => 0,
            DemoTaskStatus.InProgress => 1,
            DemoTaskStatus.Blocked    => 2,
            DemoTaskStatus.Done       => 3,
            DemoTaskStatus.Cancelled  => 4,
            _                         => int.MaxValue,
        };

        return WorkflowIndex(a).CompareTo(WorkflowIndex(b));
    };

    public IReadOnlyCollection<DemoPriority> ExcludedValues { get; } = [DemoPriority.Blocker];

    public IReadOnlyCollection<DemoPriority> IncludedValues { get; } = [DemoPriority.Low, DemoPriority.Normal, DemoPriority.High];
}