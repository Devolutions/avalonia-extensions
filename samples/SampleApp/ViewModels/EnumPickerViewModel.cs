namespace SampleApp.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

using Devolutions.AvaloniaControls.Controls;

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

    // === Dynamic Demo ===

    [ObservableProperty]
    private DemoPriority? dynamicSelected;

    [ObservableProperty]
    private EnumPicker.SortOrder dynamicSortOrder;

    public IReadOnlyCollection<EnumPicker.SortOrder> SortOrderValues { get; } = Enum.GetValues<EnumPicker.SortOrder>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicTextProvider))]
    private bool dynamicUseTextProvider;

    public Func<object, string>? DynamicTextProvider => DynamicUseTextProvider ? TextProvider : null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicTextOverrides))]
    private bool dynamicUseTextOverrides;

    public IReadOnlyDictionary<DemoPriority, string>? DynamicTextOverrides => DynamicUseTextOverrides ? TextOverrides : null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicCustomSort))]
    private bool dynamicUseCustomSort;

    private static readonly Comparison<DemoPriority> PriorityReverseSort = (a, b) => b.CompareTo(a);

    public Comparison<DemoPriority>? DynamicCustomSort => DynamicUseCustomSort ? PriorityReverseSort : null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicIncludedValues))]
    private bool dynamicEnableIncludeFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicIncludedValues))]
    private bool dynamicIncludeLow = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicIncludedValues))]
    private bool dynamicIncludeNormal = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicIncludedValues))]
    private bool dynamicIncludeHigh = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicIncludedValues))]
    private bool dynamicIncludeCritical = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicIncludedValues))]
    private bool dynamicIncludeBlocker = true;

    public IReadOnlyCollection<DemoPriority>? DynamicIncludedValues
    {
        get
        {
            if (!DynamicEnableIncludeFilter) return null;
            var values = new List<DemoPriority>();
            if (DynamicIncludeLow)      values.Add(DemoPriority.Low);
            if (DynamicIncludeNormal)   values.Add(DemoPriority.Normal);
            if (DynamicIncludeHigh)     values.Add(DemoPriority.High);
            if (DynamicIncludeCritical) values.Add(DemoPriority.Critical);
            if (DynamicIncludeBlocker)  values.Add(DemoPriority.Blocker);
            return values;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicExcludedValues))]
    private bool dynamicExcludeLow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicExcludedValues))]
    private bool dynamicExcludeNormal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicExcludedValues))]
    private bool dynamicExcludeHigh;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicExcludedValues))]
    private bool dynamicExcludeCritical;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicExcludedValues))]
    private bool dynamicExcludeBlocker;

    public IReadOnlyCollection<DemoPriority>? DynamicExcludedValues
    {
        get
        {
            var values = new List<DemoPriority>();
            if (DynamicExcludeLow)      values.Add(DemoPriority.Low);
            if (DynamicExcludeNormal)   values.Add(DemoPriority.Normal);
            if (DynamicExcludeHigh)     values.Add(DemoPriority.High);
            if (DynamicExcludeCritical) values.Add(DemoPriority.Critical);
            if (DynamicExcludeBlocker)  values.Add(DemoPriority.Blocker);
            return values.Count > 0 ? values : null;
        }
    }
}