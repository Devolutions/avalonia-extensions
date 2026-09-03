namespace Devolutions.AvaloniaControls.Tests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Devolutions.AvaloniaControls.Helpers;

public class BindingEvaluatorTests
{
    [AvaloniaFact]
    public void CompiledMultiSegmentFormattedGetterReturnsValue()
    {
        CompiledBinding binding = CreateNameBinding();
        var row = new TestRow { Customer = new TestCustomer { Name = "Alice" } };

        Assert.Equal("Alice", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Alice", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [AvaloniaFact]
    public void CompiledMultiSegmentFormattedGetterHandlesNullIntermediate()
    {
        CompiledBinding binding = CreateNameBinding();
        var row = new TestRow();

        Assert.Equal(string.Empty, CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal(string.Empty, CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [AvaloniaFact]
    public void CompiledMultiSegmentRawGetterReturnsUnsetForNullIntermediate()
    {
        CompiledBinding binding = CreateNameBinding();
        var row = new TestRow();

        Assert.Same(AvaloniaProperty.UnsetValue, CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Same(AvaloniaProperty.UnsetValue, CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void CompiledMultiSegmentFormattedGetterUsesFallbackForNullIntermediate()
    {
        CompiledBinding binding = CreateNameBinding();
        binding.FallbackValue = "Unknown";
        var row = new TestRow();

        Assert.Equal("Unknown", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Unknown", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [AvaloniaFact]
    public void CompiledThreePartPathReturnsValue()
    {
        CompiledBinding binding = CreateCityBinding();
        var row = new TestRow
        {
            Customer = new TestCustomer { Address = new TestAddress { City = "Montreal" } }
        };

        Assert.Equal("Montreal", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Montreal", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
        Assert.Equal("Montreal", CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Equal("Montreal", CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void CompiledThreePartPathHandlesNullMiddleSegment()
    {
        CompiledBinding binding = CreateCityBinding();
        var row = new TestRow { Customer = new TestCustomer() };

        Assert.Equal(string.Empty, CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal(string.Empty, CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
        Assert.Same(AvaloniaProperty.UnsetValue, CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Same(AvaloniaProperty.UnsetValue, CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void CompiledThreePartPathUsesFallbackWhenMiddleSegmentIsNull()
    {
        CompiledBinding binding = CreateCityBinding();
        binding.FallbackValue = "Unknown";
        var row = new TestRow { Customer = new TestCustomer() };

        Assert.Equal("Unknown", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Unknown", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
        Assert.Equal("Unknown", CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Equal("Unknown", CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void CompiledThreePartPathUsesTargetNullValueWhenLeafIsNull()
    {
        CompiledBinding binding = CreateCityBinding();
        binding.TargetNullValue = "No city";
        var row = new TestRow
        {
            Customer = new TestCustomer { Address = new TestAddress() }
        };

        Assert.Equal("No city", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("No city", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
        Assert.Equal("No city", CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Equal("No city", CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void ReflectionMultiSegmentFormattedGetterReturnsValue()
    {
        Binding binding = CreateReflectionNameBinding();
        var row = new TestRow { Customer = new TestCustomer { Name = "Alice" } };

        Assert.Equal("Alice", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Alice", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [AvaloniaFact]
    public void ReflectionMultiSegmentFormattedGetterHandlesNullIntermediate()
    {
        Binding binding = CreateReflectionNameBinding();
        var row = new TestRow();

        Assert.Equal(string.Empty, CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal(string.Empty, CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [AvaloniaFact]
    public void ReflectionMultiSegmentRawGetterReturnsUnsetForNullIntermediate()
    {
        Binding binding = CreateReflectionNameBinding();
        var row = new TestRow();

        Assert.Same(AvaloniaProperty.UnsetValue, CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Same(AvaloniaProperty.UnsetValue, CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void ReflectionMultiSegmentFormattedGetterUsesFallbackForNullIntermediate()
    {
        Binding binding = CreateReflectionNameBinding();
        binding.FallbackValue = "Unknown";
        var row = new TestRow();

        Assert.Equal("Unknown", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Unknown", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
    }

    [AvaloniaFact]
    public void ReflectionThreePartPathReturnsValue()
    {
        Binding binding = CreateReflectionCityBinding();
        var row = new TestRow
        {
            Customer = new TestCustomer { Address = new TestAddress { City = "Montreal" } }
        };

        Assert.Equal("Montreal", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Montreal", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
        Assert.Equal("Montreal", CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Equal("Montreal", CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void ReflectionThreePartPathHandlesNullMiddleSegment()
    {
        Binding binding = CreateReflectionCityBinding();
        var row = new TestRow { Customer = new TestCustomer() };

        Assert.Equal(string.Empty, CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal(string.Empty, CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
        Assert.Same(AvaloniaProperty.UnsetValue, CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Same(AvaloniaProperty.UnsetValue, CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void ReflectionThreePartPathUsesFallbackWhenMiddleSegmentIsNull()
    {
        Binding binding = CreateReflectionCityBinding();
        binding.FallbackValue = "Unknown";
        var row = new TestRow { Customer = new TestCustomer() };

        Assert.Equal("Unknown", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("Unknown", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
        Assert.Equal("Unknown", CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Equal("Unknown", CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    [AvaloniaFact]
    public void ReflectionThreePartPathUsesTargetNullValueWhenLeafIsNull()
    {
        Binding binding = CreateReflectionCityBinding();
        binding.TargetNullValue = "No city";
        var row = new TestRow
        {
            Customer = new TestCustomer { Address = new TestAddress() }
        };

        Assert.Equal("No city", CreateEvaluator().BuildFormattedGetter(binding)!(row));
        Assert.Equal("No city", CreateTypedEvaluator().BuildFormattedGetterExpression(binding)!.Compile()(row));
        Assert.Equal("No city", CreateEvaluator().BuildRawGetter(binding)(row));
        Assert.Equal("No city", CreateTypedEvaluator().BuildRawGetterExpression(binding).Compile()(row));
    }

    private static CompiledBinding CreateNameBinding() =>
        CompiledBinding.Create((TestRow row) => row.Customer!.Name);

    private static CompiledBinding CreateCityBinding() =>
        CompiledBinding.Create((TestRow row) => row.Customer!.Address!.City);

    private static Binding CreateReflectionNameBinding() => new("Customer.Name");

    private static Binding CreateReflectionCityBinding() => new("Customer.Address.City");

    private static BindingEvaluator CreateEvaluator() => new(new Control(), typeof(TestRow));

    private static BindingEvaluator<TestRow> CreateTypedEvaluator() => new(new Control());

    private sealed class TestRow
    {
        public TestCustomer? Customer { get; init; }
    }

    private sealed class TestCustomer
    {
        public string? Name { get; init; }

        public TestAddress? Address { get; init; }
    }

    private sealed class TestAddress
    {
        public string? City { get; init; }
    }
}
