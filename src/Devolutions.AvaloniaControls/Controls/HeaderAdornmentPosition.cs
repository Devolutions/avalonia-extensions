namespace Devolutions.AvaloniaControls.Controls;

/// <summary>
/// Where a column header's adornment sits, see
/// <see cref="DevoTreeDataGridExtensions.HeaderAdornmentPositionProperty"/>.
/// </summary>
public enum HeaderAdornmentPosition
{
    /// <summary>
    /// Pinned to the right edge of the header, after the sort indicator, sized to its own content. The
    /// header caption ellipsizes to make room for it.
    /// </summary>
    Right,

    /// <summary>
    /// Spanning the whole header, hiding both the caption and the sort indicator. Suits an adornment that
    /// takes the cell over, such as an in-column search field.
    /// </summary>
    /// <remarks>
    /// A filling adornment reaches the header's own edges, ignoring the caption insets a theme applies
    /// through <c>Padding</c>. That is deliberate, so a consumer can style the whole cell, but it has a
    /// known consequence in the DevExpress theme: that theme's resize thumb overhangs 6px into the
    /// following header, and a hit-testable filling adornment there covers that lane, so the previous
    /// column can no longer be resized by grabbing its right edge. Themes whose resizer stays inside its
    /// own header, such as MacOS and Linux, are unaffected.
    /// </remarks>
    Fill,
}
