namespace Devolutions.AvaloniaControls.MarkupExtensions;

using Avalonia.Data.Converters;

public class AndBinding : AbstractMultipleValueBinding<bool>
{
    protected override IMultiValueConverter MultiValueConverter => BoolConverters.And;

    // @formatter:off
    public AndBinding(object b1, object b2) : base(b1, b2) { }
    public AndBinding(object b1, object b2, object b3) : base(b1, b2, b3) { }
    public AndBinding(object b1, object b2, object b3, object b4) : base(b1, b2, b3, b4) { }
    public AndBinding(object b1, object b2, object b3, object b4, object b5) : base(b1, b2, b3, b4, b5) { }
    public AndBinding(object b1, object b2, object b3, object b4, object b5, object b6) : base(b1, b2, b3, b4, b5, b6) { }
    public AndBinding(object b1, object b2, object b3, object b4, object b5, object b6, object b7) : base(b1, b2, b3, b4, b5, b6, b7) { }
    public AndBinding(object b1, object b2, object b3, object b4, object b5, object b6, object b7, object b8) : base(b1, b2, b3, b4, b5, b6, b7, b8) { }
    public AndBinding(object b1, object b2, object b3, object b4, object b5, object b6, object b7, object b8, object b9) : base(b1, b2, b3, b4, b5, b6, b7, b8, b9) { }
    // @formatter:on
}