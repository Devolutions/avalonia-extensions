namespace Devolutions.AvaloniaControls.VisualTests;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Devolutions.AvaloniaControls.Controls;

public class EnumPickerTests
{
    private enum TestEnum
    {
        Valid,
        Invalid
    }

    [AvaloniaFact]
    public void NullExcludedValuesSkipsUpdateUntilRestored()
    {
        var picker = new EnumPicker<TestEnum>
        {
            ExcludedValues = [TestEnum.Invalid],
            TextProvider = value => value switch
            {
                TestEnum.Invalid => throw new InvalidOperationException("Invalid is excluded from this provider."),
                _ => value.ToString()
            }
        };

        var window = new Window { Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        picker.ExcludedValues = null!;
        Dispatcher.UIThread.RunJobs();

        picker.ExcludedValues = [TestEnum.Invalid];
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void NullIncludedValuesSkipsUpdateUntilRestored()
    {
        var picker = new EnumPicker<TestEnum>
        {
            IncludedValues = [TestEnum.Valid],
            TextProvider = value => value switch
            {
                TestEnum.Invalid => throw new InvalidOperationException("Invalid is not included for this provider."),
                _ => value.ToString()
            }
        };

        var window = new Window { Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        picker.IncludedValues = null!;
        Dispatcher.UIThread.RunJobs();

        picker.IncludedValues = [TestEnum.Valid];
        Dispatcher.UIThread.RunJobs();
    }
}
