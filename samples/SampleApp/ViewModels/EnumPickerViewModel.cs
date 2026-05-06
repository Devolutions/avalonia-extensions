namespace SampleApp.ViewModels;

using System.Diagnostics;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Devolutions.AvaloniaControls.Controls;
using Devolutions.AvaloniaControls.Extensions;

public enum DemoStatus
{
    Active,
    Inactive,
    Pending,
    Archived
}

// Declared alphabetically (as is common in codebases), but displayed in workflow order via CustomSort
public enum DemoTaskStatus
{
    Blocked,
    Cancelled,
    Done,
    InProgress,
    Todo
}

public enum DemoPriority
{
    Low,
    Normal,
    High,
    Critical,
    Blocker
}

public enum DemoConnectionQuality
{
    Default,
    Low,
    Medium,
    High
}

public partial class EnumPickerViewModel : ObservableObject
{
    private static readonly Comparison<Enum> PriorityReverseSort = (a, b) => b.CompareTo(a);

    [ObservableProperty]
    private bool dynamicEnableIncludeFilter;

    [ObservableProperty]
    private bool dynamicExcludeBlocker;

    [ObservableProperty]
    private bool dynamicExcludeCritical;

    [ObservableProperty]
    private bool dynamicExcludeHigh;

    [ObservableProperty]
    private bool dynamicExcludeLow;

    [ObservableProperty]
    private bool dynamicExcludeNormal;

    [ObservableProperty]
    private bool dynamicIncludeBlocker = true;

    [ObservableProperty]
    private bool dynamicIncludeCritical = true;

    [ObservableProperty]
    private bool dynamicIncludeHigh = true;

    [ObservableProperty]
    private bool dynamicIncludeLow = true;

    [ObservableProperty]
    private bool dynamicIncludeNormal = true;

    // === Dynamic Demo ===

    [ObservableProperty]
    private DemoPriority dynamicSelected = DemoPriority.Low;

    [ObservableProperty]
    private EnumPicker.SortOrder dynamicSortOrder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicCustomSort))]
    private bool dynamicUseCustomSort;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicTextOverrides))]
    private bool dynamicUseTextOverrides;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicTextProvider))]
    private bool dynamicUseTextProvider;

    [ObservableProperty]
    private DemoTaskStatus selectedCustomSort = DemoTaskStatus.Blocked;

    [ObservableProperty]
    private DemoPriority selectedDefault = DemoPriority.Critical;

    [ObservableProperty]
    private DemoPriority selectedExcluded = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedIncluded = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedInlineExcluded = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedInlineIncluded = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedInlineOverrides = DemoPriority.Low;

    [ObservableProperty]
    private DemoConnectionQuality selectedProxiedOverrides = DemoConnectionQuality.Default;

    [ObservableProperty]
    private DemoPriority selectedSorted = DemoPriority.Low;

    [ObservableProperty]
    private DemoStatus selectedStatus = DemoStatus.Active;

    [ObservableProperty]
    private DemoPriority selectedWithOverrides = DemoPriority.Low;

    [ObservableProperty]
    private DemoPriority selectedWithProvider = DemoPriority.Low;

    // === Bound TextOverride Demo ===

    [ObservableProperty]
    private string highOverrideText = "High (live)";

    [ObservableProperty]
    private DemoConnectionQuality defaultQuality = DemoConnectionQuality.Medium;

    [ObservableProperty]
    private DemoPriority selectedBoundDirect = DemoPriority.High;

    [ObservableProperty]
    private DemoConnectionQuality selectedBoundFormatted = DemoConnectionQuality.Default;

    public Func<Enum, string> TextProvider { get; } = priority => priority switch
    {
        DemoPriority.Low => "Low ↓",
        DemoPriority.Normal => "Normal →",
        DemoPriority.High => "High ↑",
        DemoPriority.Critical => "Critical !!",
        DemoPriority.Blocker => "Blocker ✗",
        _ => priority.ToString()
    };

    public AvaloniaList<EnumPickerTextOverride<DemoPriority>> TextOverrides { get; } =
    [
        new EnumPickerDirectTextOverride<DemoPriority> { Enum = DemoPriority.High, Text = "High (overridden)" },
        new EnumPickerDirectTextOverride<DemoPriority> { Enum = DemoPriority.Critical, Text = "CRITICAL" }
    ];

    // Enum is alphabetical in code, but displayed in workflow order
    public Comparison<Enum>? CustomSort { get; } = (a, b) =>
    {
        static int WorkflowIndex(Enum s)
        {
            return s switch
            {
                DemoTaskStatus.Todo => 0,
                DemoTaskStatus.InProgress => 1,
                DemoTaskStatus.Blocked => 2,
                DemoTaskStatus.Done => 3,
                DemoTaskStatus.Cancelled => 4,
                _ => int.MaxValue
            };
        }

        return WorkflowIndex(a).CompareTo(WorkflowIndex(b));
    };

    public AvaloniaList<DemoPriority> ExcludedValues { get; } = [DemoPriority.Blocker];

    public AvaloniaList<DemoPriority> IncludedValues { get; } = [DemoPriority.Low, DemoPriority.Normal, DemoPriority.High];

    public IReadOnlyCollection<EnumPicker.SortOrder> SortOrderValues { get; } = Enum.GetValues<EnumPicker.SortOrder>();

    public Func<Enum, string>? DynamicTextProvider => this.DynamicUseTextProvider ? this.TextProvider : null;

    public AvaloniaList<EnumPickerTextOverride<DemoPriority>>? DynamicTextOverrides => this.DynamicUseTextOverrides ? this.TextOverrides : null;

    public Comparison<Enum>? DynamicCustomSort => this.DynamicUseCustomSort ? PriorityReverseSort : null;

    public AvaloniaList<DemoPriority> DynamicIncludedValues { get; } = [];

    public AvaloniaList<DemoPriority> DynamicExcludedValues { get; } = [];

    [RelayCommand]
    private void SetDynamicSelectedToLow() => this.DynamicSelected = DemoPriority.Low;

    [RelayCommand]
    private void SetDynamicSelectedToNormal() => this.DynamicSelected = DemoPriority.Normal;

    [RelayCommand]
    private void SetDynamicSelectedToHigh() => this.DynamicSelected = DemoPriority.High;

    [RelayCommand]
    private void SetDynamicSelectedToCritical() => this.DynamicSelected = DemoPriority.Critical;

    partial void OnDynamicEnableIncludeFilterChanged(bool value)
    {
        this.DynamicIncludedValues.Clear();
        if (value)
        {
            this.DynamicIncludedValues.AddRange(
                ((IEnumerable<DemoPriority?>)
                [
                    this.DynamicIncludeLow ? DemoPriority.Low : null,
                    this.DynamicIncludeNormal ? DemoPriority.Normal : null,
                    this.DynamicIncludeHigh ? DemoPriority.High : null,
                    this.DynamicIncludeCritical ? DemoPriority.Critical : null,
                    this.DynamicIncludeBlocker ? DemoPriority.Blocker : null
                ]).SkipNulls());
        }
    }

    partial void OnDynamicIncludeLowChanged(bool value)
    {
        if (this.DynamicEnableIncludeFilter)
        {
            SyncListValue(this.DynamicIncludedValues, DemoPriority.Low, value);
        }
    }

    partial void OnDynamicIncludeNormalChanged(bool value)
    {
        if (this.DynamicEnableIncludeFilter)
        {
            SyncListValue(this.DynamicIncludedValues, DemoPriority.Normal, value);
        }
    }

    partial void OnDynamicIncludeHighChanged(bool value)
    {
        if (this.DynamicEnableIncludeFilter)
        {
            SyncListValue(this.DynamicIncludedValues, DemoPriority.High, value);
        }
    }

    partial void OnDynamicIncludeCriticalChanged(bool value)
    {
        if (this.DynamicEnableIncludeFilter)
        {
            SyncListValue(this.DynamicIncludedValues, DemoPriority.Critical, value);
        }
    }

    partial void OnDynamicIncludeBlockerChanged(bool value)
    {
        if (this.DynamicEnableIncludeFilter)
        {
            SyncListValue(this.DynamicIncludedValues, DemoPriority.Blocker, value);
        }
    }

    partial void OnDynamicExcludeLowChanged(bool value) => SyncListValue(this.DynamicExcludedValues, DemoPriority.Low, value);

    partial void OnDynamicExcludeNormalChanged(bool value) => SyncListValue(this.DynamicExcludedValues, DemoPriority.Normal, value);

    partial void OnDynamicExcludeHighChanged(bool value) => SyncListValue(this.DynamicExcludedValues, DemoPriority.High, value);

    partial void OnDynamicExcludeCriticalChanged(bool value) => SyncListValue(this.DynamicExcludedValues, DemoPriority.Critical, value);

    partial void OnDynamicExcludeBlockerChanged(bool value) => SyncListValue(this.DynamicExcludedValues, DemoPriority.Blocker, value);

    partial void OnSelectedDefaultChanged(DemoPriority value)
    {
        Debug.WriteLine($"{nameof(this.SelectedDefault)} changed: {value}");
    }

    private static void SyncListValue(AvaloniaList<DemoPriority> list, DemoPriority value, bool add)
    {
        if (add)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }
        else
        {
            list.Remove(value);
        }
    }
}