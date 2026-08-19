namespace Devolutions.AvaloniaControls.VisualTests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Devolutions.AvaloniaControls.Helpers;

public class BindingEvaluatorTests
{
    [Fact]
    public void CompiledMultiSegmentFormattedGetterReturnsValue()
    {
        CompiledBinding binding = CreateNameBinding();
        var row = new TestRow { Customer = new TestCustomer { Name = "Alice" } };

        Assert.Equal("Alice", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Alice", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [Fact]
    public void CompiledMultiSegmentFormattedGetterHandlesNullIntermediate()
    {
        CompiledBinding binding = CreateNameBinding();
        var row = new TestRow();

        Assert.Equal(string.Empty, CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal(string.Empty, CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [Fact]
    public void CompiledMultiSegmentRawGetterReturnsUnsetForNullIntermediate()
    {
        CompiledBinding binding = CreateNameBinding();
        var row = new TestRow();

        Assert.Same(AvaloniaProperty.UnsetValue, CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Same(AvaloniaProperty.UnsetValue, CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [Fact]
    public void CompiledMultiSegmentFormattedGetterUsesFallbackForNullIntermediate()
    {
        CompiledBinding binding = CreateNameBinding();
        binding.FallbackValue = "Unknown";
        var row = new TestRow();

        Assert.Equal("Unknown", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Unknown", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [Fact]
    public void ReflectionMultiSegmentFormattedGetterReturnsValue()
    {
        Binding binding = CreateReflectionNameBinding();
        var row = new TestRow { Customer = new TestCustomer { Name = "Alice" } };

        Assert.Equal("Alice", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Alice", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [Fact]
    public void ReflectionMultiSegmentFormattedGetterHandlesNullIntermediate()
    {
        Binding binding = CreateReflectionNameBinding();
        var row = new TestRow();

        Assert.Equal(string.Empty, CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal(string.Empty, CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [Fact]
    public void ReflectionMultiSegmentRawGetterReturnsUnsetForNullIntermediate()
    {
        Binding binding = CreateReflectionNameBinding();
        var row = new TestRow();

        Assert.Same(AvaloniaProperty.UnsetValue, CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Same(AvaloniaProperty.UnsetValue, CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [Fact]
    public void ReflectionMultiSegmentFormattedGetterUsesFallbackForNullIntermediate()
    {
        Binding binding = CreateReflectionNameBinding();
        binding.FallbackValue = "Unknown";
        var row = new TestRow();

        Assert.Equal("Unknown", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Unknown", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    private static CompiledBinding CreateNameBinding() =>
        CompiledBinding.Create((TestRow row) => row.Customer!.Name);

    private static Binding CreateReflectionNameBinding() => new("Customer.Name");

    private static BindingEvaluator CreateEvaluator() => new(new Control(), typeof(TestRow));

    private static BindingEvaluator<TestRow> CreateTypedEvaluator() => new(new Control());

    private sealed class TestRow
    {
        public TestCustomer? Customer { get; init; }
    }

    private sealed class TestCustomer
    {
        public string? Name { get; init; }
    }
}
