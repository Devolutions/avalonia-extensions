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
    Fill,
}
