namespace SampleApp.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private DemoPriority selectedDefault = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedSorted = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedExcluded = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedIncluded = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedWithOverrides = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedWithProvider = DemoPriority.Low;

    [ObservableProperty]
    private DemoStatus selectedStatus = DemoStatus.Active;

    [ObservableProperty]
    private DemoPriority selectedInlineExcluded = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedInlineIncluded = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedInlineOverrides = DemoPriority.Low;

    [ObservableProperty]
    private DemoTaskStatus selectedCustomSort = DemoTaskStatus.Blocked;

    public Func<Enum, string> TextProvider { get; } = priority => priority switch
    {
        DemoPriority.Low => "Low ↓",
        DemoPriority.Normal => "Normal →",
        DemoPriority.High => "High ↑",
        DemoPriority.Critical => "Critical !!",
        DemoPriority.Blocker => "Blocker ✗",
        _ => priority.ToString()
    };

    public IReadOnlyCollection<EnumPickerTextOverride<DemoPriority>> TextOverrides { get; } =
    [
        new EnumPickerDirectTextOverride<DemoPriority> { Enum = DemoPriority.High, Text = "High (overridden)" },
        new EnumPickerDirectTextOverride<DemoPriority> { Enum = DemoPriority.Critical, Text = "CRITICAL" }
    ];

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
    private DemoPriority dynamicSelected = DemoPriority.Low;

    [RelayCommand]
    private void SetDynamicSelectedToLow() => this.DynamicSelected = DemoPriority.Low;
    
    [RelayCommand]
    private void SetDynamicSelectedToNormal() => this.DynamicSelected = DemoPriority.Normal;
    
    [RelayCommand]
    private void SetDynamicSelectedToHigh() => this.DynamicSelected = DemoPriority.High;
    
    [RelayCommand]
    private void SetDynamicSelectedToCritical() => this.DynamicSelected = DemoPriority.Critical;

    [ObservableProperty]
    private EnumPicker.SortOrder dynamicSortOrder;

    public IReadOnlyCollection<EnumPicker.SortOrder> SortOrderValues { get; } = Enum.GetValues<EnumPicker.SortOrder>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicTextProvider))]
    private bool dynamicUseTextProvider;

    public Func<Enum, string>? DynamicTextProvider => this.DynamicUseTextProvider ? this.TextProvider : null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicTextOverrides))]
    private bool dynamicUseTextOverrides;

    public IReadOnlyCollection<EnumPickerTextOverride<DemoPriority>>? DynamicTextOverrides => this.DynamicUseTextOverrides ? this.TextOverrides : null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicCustomSort))]
    private bool dynamicUseCustomSort;

    private static readonly Comparison<DemoPriority> PriorityReverseSort = (a, b) => b.CompareTo(a);

    public Comparison<DemoPriority>? DynamicCustomSort => this.DynamicUseCustomSort ? PriorityReverseSort : null;

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
            if (!this.DynamicEnableIncludeFilter) return null;
            var values = new List<DemoPriority>();
            if (this.DynamicIncludeLow)      values.Add(DemoPriority.Low);
            if (this.DynamicIncludeNormal)   values.Add(DemoPriority.Normal);
            if (this.DynamicIncludeHigh)     values.Add(DemoPriority.High);
            if (this.DynamicIncludeCritical) values.Add(DemoPriority.Critical);
            if (this.DynamicIncludeBlocker)  values.Add(DemoPriority.Blocker);
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
            if (this.DynamicExcludeLow)      values.Add(DemoPriority.Low);
            if (this.DynamicExcludeNormal)   values.Add(DemoPriority.Normal);
            if (this.DynamicExcludeHigh)     values.Add(DemoPriority.High);
            if (this.DynamicExcludeCritical) values.Add(DemoPriority.Critical);
            if (this.DynamicExcludeBlocker)  values.Add(DemoPriority.Blocker);
            return values.Count > 0 ? values : null;
        }
    }
}