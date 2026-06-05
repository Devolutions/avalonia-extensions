using Avalonia.Controls.Primitives;

namespace Devolutions.AvaloniaTheme.DevExpress;

/// <summary>
/// A lightweight control that renders the 4-layer focus ring used by the DevExpress theme.
/// All visual layers are driven by <see cref="TemplatedControl.BorderThickness"/> and
/// <see cref="TemplatedControl.CornerRadius"/>, so open-sided rings (e.g. "2 2 2 0") work
/// naturally — every layer uses the same thickness on the same sides.
/// </summary>
internal sealed class FocusHighlight : TemplatedControl
{
}
