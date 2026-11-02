namespace Devolutions.AvaloniaControls.Tests;

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Devolutions.AvaloniaControls.Controls;
using SampleApp;

[Collection("StylesTest")]
public class EnumPickerTests
{
    private enum TestEnum
    {
        Valid,
        Invalid
    }

    private enum PrimeTestEnum
    {
        Alpha,
        Beta,
        Gamma
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
    public void PrimeItemsWithoutTemplateKeepsOffscreenItemsUpdated()
    {
        var typedPicker = new EnumPicker<PrimeTestEnum>
        {
            IncludedValues = [PrimeTestEnum.Alpha, PrimeTestEnum.Gamma],
            CustomSort = (left, right) => right.CompareTo(left),
            TextProvider = value => $"Initial {value}"
        };
        EnumPicker picker = typedPicker;

        picker.PrimeItemsWithoutTemplate();
        picker.PrimeItemsWithoutTemplate();

        Assert.Equal(
            [PrimeTestEnum.Gamma, PrimeTestEnum.Alpha],
            picker.Items.Select(item => (PrimeTestEnum)item.EnumValue));
        Assert.Equal(["Initial Gamma", "Initial Alpha"], picker.Items.Select(item => item.Text));

        typedPicker.ExcludedValues = [PrimeTestEnum.Gamma];
        Assert.Equal(PrimeTestEnum.Alpha, Assert.Single(picker.Items).EnumValue);

        typedPicker.TextProvider = value => $"Updated {value}";
        Assert.Equal("Updated Alpha", Assert.Single(picker.Items).Text);

        typedPicker.TextOverrides.Add(new EnumPickerDirectTextOverride<PrimeTestEnum>
        {
            Enum = PrimeTestEnum.Alpha,
            Text = "Overridden Alpha"
        });
        Assert.Equal("Overridden Alpha", Assert.Single(picker.Items).Text);
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

    [AvaloniaFact]
    public void ReattachingKeepsBoundSelectedValue()
    {
        ResetTheme();

        var viewModel = new SelectedValueViewModel { Value = PrimeTestEnum.Gamma };
        var picker = new EnumPicker<PrimeTestEnum> { IncludedValues = [PrimeTestEnum.Alpha, PrimeTestEnum.Gamma] };
        picker.Bind(EnumPicker<PrimeTestEnum>.SelectedValueProperty, new ReflectionBinding(nameof(SelectedValueViewModel.Value)) { Mode = BindingMode.TwoWay });

        // Mirrors a cached page swapped in and out of a ContentControl, like a settings dialog switching sections.
        var page = new Border { DataContext = viewModel, Child = picker };
        var host = new ContentControl { Content = page };
        var window = new Window { Content = host };

        // Style-provided inputs are removed on detach and re-applied on attach, so each reattach rebuilds the items.
        window.Styles.Add(new Style(static x => x.Is<EnumPicker>())
        {
            Setters =
            {
                new Setter(EnumPicker.TextProviderProperty, new Func<Enum, string>(static value => $"Styled {value}")),
                new Setter(EnumPicker.CustomSortProperty, new Comparison<Enum>(static (left, right) => right.CompareTo(left))),
            },
        });
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(PrimeTestEnum.Gamma, picker.SelectedValue);
            viewModel.Writes.Clear();

            host.Content = null;
            Dispatcher.UIThread.RunJobs();

            host.Content = page;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(PrimeTestEnum.Gamma, viewModel.Value);
            Assert.Equal(PrimeTestEnum.Gamma, picker.SelectedValue);
            Assert.Empty(viewModel.Writes);
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void ExcludingEveryValueResetsBoundSelectedValue()
    {
        ResetTheme();

        var viewModel = new SelectedValueViewModel { Value = PrimeTestEnum.Gamma };
        var picker = new EnumPicker<PrimeTestEnum> { DataContext = viewModel };
        picker.Bind(EnumPicker<PrimeTestEnum>.SelectedValueProperty, new ReflectionBinding(nameof(SelectedValueViewModel.Value)) { Mode = BindingMode.TwoWay });

        var window = new Window { Content = picker };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(PrimeTestEnum.Gamma, picker.SelectedValue);

            picker.ExcludedValues = [PrimeTestEnum.Alpha, PrimeTestEnum.Beta, PrimeTestEnum.Gamma];
            Dispatcher.UIThread.RunJobs();

            // An empty picker has nothing selected, so the bound value must fall back to default(T).
            Assert.Empty(picker.Items);
            Assert.Equal(default, picker.SelectedValue);
            Assert.Equal(default, viewModel.Value);
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void ExcludingSelectedValueMovesBoundSelectedValueToFirstItem()
    {
        ResetTheme();

        var viewModel = new SelectedValueViewModel { Value = PrimeTestEnum.Gamma };
        var picker = new EnumPicker<PrimeTestEnum> { DataContext = viewModel };
        picker.Bind(EnumPicker<PrimeTestEnum>.SelectedValueProperty, new ReflectionBinding(nameof(SelectedValueViewModel.Value)) { Mode = BindingMode.TwoWay });

        var window = new Window { Content = picker };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            picker.ExcludedValues = [PrimeTestEnum.Alpha, PrimeTestEnum.Gamma];
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(PrimeTestEnum.Beta, picker.SelectedValue);
            Assert.Equal(PrimeTestEnum.Beta, viewModel.Value);
        }
        finally
        {
            window.Close();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    private sealed class SelectedValueViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public PrimeTestEnum Value
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                this.Writes.Add(value);
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Value)));
            }
        }

        public List<PrimeTestEnum> Writes { get; } = [];
    }
}
