namespace SampleApp.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

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
    public Func<DemoPriority, string> TextProvider { get; } = priority => priority switch
    {
        DemoPriority.Low => "Low ↓",
        DemoPriority.Normal => "Normal →",
        DemoPriority.High => "High ↑",
        DemoPriority.Critical => "Critical !!",
        DemoPriority.Blocker => "Blocker ✗",
        _ => priority.ToString()
    };

    public IReadOnlyDictionary<DemoPriority, string> TextOverrides { get; } = new Dictionary<DemoPriority, string>
    {
        { DemoPriority.High, "High (overridden)" },
        { DemoPriority.Critical, "CRITICAL" }
    };

    public IReadOnlyCollection<DemoPriority> ExcludedValues { get; } = [DemoPriority.Blocker];

    public IReadOnlyCollection<DemoPriority> IncludedValues { get; } = [DemoPriority.Low, DemoPriority.Normal, DemoPriority.High];
}