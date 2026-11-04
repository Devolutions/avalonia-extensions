namespace Devolutions.AvaloniaControls.Tests;

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Devolutions.AvaloniaControls.Helpers;
using Devolutions.AvaloniaControls.MarkupExtensions;

/// <summary>
/// Covers how markup extensions turn XAML literals into bindings.
///
/// Two properties matter here and neither was covered before:
/// <list type="number">
/// <item>Parsing is invariant, always. The whole solution runs in globalization-invariant mode
/// (<c>InvariantGlobalization</c> in Directory.Build.props) and every conversion names
/// <see cref="CultureInfo.InvariantCulture"/> explicitly, enforced by the globalization rules in
/// .editorconfig. There is no culture to handle here and nothing should ever introduce one.</item>
/// <item>Resolving a literal must not need runtime code generation. The <c>dynamic</c> fallback builds a
/// call site on first use, which loads Microsoft.CSharp and System.Linq.Expressions and emits a
/// DynamicMethod on the UI thread during the first layout pass that touches the template.</item>
/// </list>
/// </summary>
public class MarkupExtensionHelpersTests
{
    /// <summary>
    /// Pins the solution-wide invariant policy at runtime. If this fails, something dropped
    /// <c>InvariantGlobalization</c> and behaviour can start depending on the machine's locale again.
    /// </summary>
    [Fact]
    public void Everything_runs_in_globalization_invariant_mode()
    {
        Assert.True(
            AppContext.TryGetSwitch("System.Globalization.Invariant", out bool invariant) && invariant,
            "Expected globalization-invariant mode. Check InvariantGlobalization in Directory.Build.props.");

        Assert.Equal(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
    }

    [AvaloniaTheory]
    [InlineData("-1", -1d)]
    [InlineData("0.25", 0.25d)]
    [InlineData("-4", -4d)]
    [InlineData("12", 12d)]
    public void Double_literals_parse_to_their_invariant_value(string literal, double expected)
    {
        // "-1" is the literal from the TreeDataGridOverflowHeader templates;
        // "0.25" is the one from the DevExpress ChangeColorOpacity resources.
        Assert.Equal(expected, Evaluate(MarkupExtensionHelpers.GetParsableBinding<double>(literal)));
        Assert.Equal(expected, Evaluate(MarkupExtensionHelpers.GetBinding<double>(literal)));
        Assert.Equal(expected, Evaluate(MarkupExtensionHelpers.GetBindingForPropertyType(literal, typeof(double))));
    }

    /// <summary>
    /// Regression: System.Boolean implements <c>IParsable&lt;bool&gt;.Parse</c> as an *explicit* interface
    /// implementation, so a <c>GetMethod("Parse", [string, IFormatProvider])</c> lookup finds nothing. The
    /// reflection fallback therefore threw for bool, and the Type-based overload silently bound null.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("true", true)]
    public void Bool_literals_resolve_through_every_entry_point(string literal, bool expected)
    {
        Assert.Equal(expected, Evaluate(MarkupExtensionHelpers.GetParsableBinding<bool>(literal)));
        Assert.Equal(expected, Evaluate(MarkupExtensionHelpers.GetBinding<bool>(literal)));
        Assert.Equal(expected, Evaluate(MarkupExtensionHelpers.GetBindingForPropertyType(literal, typeof(bool))));
    }

    /// <summary>The constrained path and the unconstrained one must agree for every primitive.</summary>
    [AvaloniaFact]
    public void Constrained_and_unconstrained_paths_agree()
    {
        AssertAgree<double>("-1");
        AssertAgree<double>("0.25");
        AssertAgree<int>("-1");
        AssertAgree<int>("42");
        AssertAgree<float>("1.5");
        AssertAgree<decimal>("-3.25");
        AssertAgree<bool>("True");
    }

    [AvaloniaFact]
    public void Malformed_literals_throw_rather_than_binding_a_default()
    {
        Assert.ThrowsAny<FormatException>(() => MarkupExtensionHelpers.GetParsableBinding<double>("not-a-number"));
        Assert.ThrowsAny<FormatException>(() => MarkupExtensionHelpers.GetBinding<double>("not-a-number"));
        Assert.ThrowsAny<FormatException>(() => MarkupExtensionHelpers.GetParsableBinding<bool>("maybe"));
    }

    /// <summary>
    /// Documents a sharp edge rather than asserting a preference: a comma in a literal is read as the
    /// invariant *group* separator, never as a decimal point, so "0,25" silently means twenty-five. That
    /// is the default NumberStyles (Float | AllowThousands) of the Parse overload IParsable uses, so both
    /// paths agree and neither can be tightened without them diverging. Never write "0,25" in XAML.
    /// </summary>
    [AvaloniaFact]
    public void Comma_is_a_group_separator_not_a_decimal_point()
    {
        Assert.Equal(25d, Evaluate(MarkupExtensionHelpers.GetParsableBinding<double>("0,25")));
        Assert.Equal(25d, Evaluate(MarkupExtensionHelpers.GetBinding<double>("0,25")));
        Assert.Equal(0.25d, Evaluate(MarkupExtensionHelpers.GetParsableBinding<double>("0.25")));
    }

    /// <summary>
    /// The shape RDM hits at startup: <c>{MultiplyBinding {Binding Width}, -1}</c> inside the
    /// TreeDataGridOverflowHeader template, negating an adornment width.
    /// </summary>
    [AvaloniaFact]
    public void MultiplyBinding_negates_a_bound_width_with_a_literal()
    {
        var source = new ContentControl { Width = 17 };

        object provided = new MultiplyBinding(
            new Binding(nameof(ContentControl.Width)) { Source = source },
            "-1").ProvideValue(EmptyServiceProvider.Instance);

        Assert.Equal(-17d, Evaluate((BindingBase)provided));
    }

    [AvaloniaFact]
    public void AddBinding_applies_a_literal_offset()
    {
        var source = new ContentControl { Width = 30 };

        object provided = new AddBinding(
            new Binding(nameof(ContentControl.Width)) { Source = source },
            "-4").ProvideValue(EmptyServiceProvider.Instance);

        Assert.Equal(26d, Evaluate((BindingBase)provided));
    }

    /// <summary>
    /// Structural guard. The constraint is what keeps literal parsing on a constrained call instead of the
    /// dynamic fallback; dropping it would silently reintroduce the startup cost with no test failing.
    /// </summary>
    [Fact]
    public void AbstractMultipleValueBinding_stays_constrained_to_IParsable()
    {
        Type parameter = typeof(AbstractMultipleValueBinding<>).GetGenericArguments()[0];

        Assert.Contains(
            parameter.GetGenericParameterConstraints(),
            c => c.IsGenericType && c.GetGenericTypeDefinition() == typeof(IParsable<>));
    }

    /// <summary>
    /// The actual startup regression. <c>ParseViaDynamic</c> is the only use of <c>dynamic</c> in the whole
    /// solution and Microsoft.CSharp is loaded by nothing else, so if the C# runtime binder is present after
    /// exercising every literal-bearing markup extension, something has fallen back to building a runtime
    /// call site again.
    ///
    /// Only Microsoft.CSharp is checked. System.Linq.Expressions is loaded as a side effect of dynamic too,
    /// but it is also used legitimately by <see cref="BindingEvaluator"/>, so its presence proves nothing.
    /// </summary>
    [AvaloniaFact]
    public void Resolving_literals_does_not_load_the_C_sharp_runtime_binder()
    {
        _ = MarkupExtensionHelpers.GetParsableBinding<double>("-1");
        _ = MarkupExtensionHelpers.GetParsableBinding<bool>("True");
        _ = MarkupExtensionHelpers.GetBinding<double>("0.25");
        _ = MarkupExtensionHelpers.GetBinding<bool>("False");
        _ = MarkupExtensionHelpers.GetBindingForPropertyType("-1", typeof(double));
        _ = new MultiplyBinding("2", "-1");
        _ = new AddBinding("2", "-4");
        _ = new AndBinding("True", "False");
        _ = new OrBinding("True", "False");

        bool binderLoaded = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "Microsoft.CSharp");

        Assert.False(binderLoaded,
            "Resolving XAML literals loaded Microsoft.CSharp, so a dynamic call site was built at runtime. " +
            "Check that the IParsable constraint and the ParseString fast paths are still in place.");
    }

    private static void AssertAgree<TIn>(string literal) where TIn : IParsable<TIn>
    {
        Assert.Equal(
            Evaluate(MarkupExtensionHelpers.GetBinding<TIn>(literal)),
            Evaluate(MarkupExtensionHelpers.GetParsableBinding<TIn>(literal)));

        Assert.Equal(TIn.Parse(literal, CultureInfo.InvariantCulture),
            Evaluate(MarkupExtensionHelpers.GetParsableBinding<TIn>(literal)));
    }

    /// <summary>Reads the value a <see cref="BindingBase"/> produces by applying it to a control.</summary>
    private static object? Evaluate(BindingBase? binding)
    {
        Assert.NotNull(binding);

        var control = new ContentControl();
        control.Bind(Control.TagProperty, binding);
        return control.Tag;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();

        public object? GetService(Type serviceType) => null;
    }
}
