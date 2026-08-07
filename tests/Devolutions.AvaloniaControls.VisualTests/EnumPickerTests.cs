namespace Devolutions.AvaloniaControls.VisualTests;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Devolutions.AvaloniaControls.Controls;
using SampleApp;

[Collection("VisualTests")]
public class EnumPickerTests
{
    private enum TestEnum
    {
        Valid,
        Invalid
    }

    /// <summary>
    ///  Reloads the theme styles so the EnumPicker control theme resolves in this test.
    ///  Without it only the first templated test in the assembly run finds a template,
    ///  and <see cref="EnumPicker{T}"/> never rebuilds its items.
    /// </summary>
    private static void ResetTheme()
    {
        App.CurrentTheme = null;
        App.SetTheme(new DevExpressTheme());
    }

    [AvaloniaFact]
    public void NullExcludedValuesSkipsUpdateUntilRestored()
    {
        ResetTheme();

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
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Single(picker.Items);

            // The provider throws on Invalid, so a null filter must not rebuild the items.
            picker.ExcludedValues = null!;
            Dispatcher.UIThread.RunJobs();

            Assert.Single(picker.Items);

            picker.ExcludedValues = [TestEnum.Invalid];
            Dispatcher.UIThread.RunJobs();

            Assert.Single(picker.Items);
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void NullIncludedValuesSkipsUpdateUntilRestored()
    {
        ResetTheme();

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
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Single(picker.Items);

            // The provider throws on Invalid, so a null filter must not rebuild the items.
            picker.IncludedValues = null!;
            Dispatcher.UIThread.RunJobs();

            Assert.Single(picker.Items);

            picker.IncludedValues = [TestEnum.Valid];
            Dispatcher.UIThread.RunJobs();

            Assert.Single(picker.Items);
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void RestoringExposedExcludedValuesInstanceResumesUpdates()
    {
        ResetTheme();

        var picker = new EnumPicker<TestEnum> { ExcludedValues = [TestEnum.Invalid] };

        var window = new Window { Content = picker };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Single(picker.Items);

            picker.ExcludedValues = null!;
            Dispatcher.UIThread.RunJobs();

            // Updates stay paused while the filter is null, so the previous items are kept.
            Assert.Single(picker.Items);

            // Assigning back the fallback list the getter exposes raises no property change,
            // yet the picker must resume updating on the invalid-to-valid transition.
            IList<TestEnum> exposed = picker.ExcludedValues;
            picker.ExcludedValues = exposed;
            Dispatcher.UIThread.RunJobs();

            // The fallback excludes nothing, so every value is listed again.
            Assert.Equal(2, picker.Items.Count);
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void RestoringExposedIncludedValuesInstanceResumesUpdates()
    {
        ResetTheme();

        var picker = new EnumPicker<TestEnum> { IncludedValues = [TestEnum.Valid] };

        var window = new Window { Content = picker };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Single(picker.Items);

            picker.IncludedValues = null!;
            Dispatcher.UIThread.RunJobs();

            // Updates stay paused while the filter is null, so the previous items are kept.
            Assert.Single(picker.Items);

            // Assigning back the fallback list the getter exposes raises no property change,
            // yet the picker must resume updating on the invalid-to-valid transition.
            IList<TestEnum> exposed = picker.IncludedValues;
            picker.IncludedValues = exposed;
            Dispatcher.UIThread.RunJobs();

            // The fallback includes everything, so every value is listed again.
            Assert.Equal(2, picker.Items.Count);
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }
}
