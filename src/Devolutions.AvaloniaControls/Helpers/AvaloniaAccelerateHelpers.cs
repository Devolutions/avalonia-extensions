using System.Diagnostics.CodeAnalysis;

namespace Devolutions.AvaloniaControls.Helpers;

public static class AvaloniaAccelerateHelpers
{
#if ENABLE_ACCELERATE
    private static bool? isTreeDataGridAvailable;

    public static bool IsTreeDataGridAvailable
    {
        get
        {
            isTreeDataGridAvailable ??= ProbeTreeDataGrid();
            return (bool)isTreeDataGridAvailable;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057",
        Justification = "TreeDataGrid assembly is preserved via TrimmerRootDescriptor in the theme projects that consume it.")]
    private static bool ProbeTreeDataGrid()
    {
        return Type.GetType("Avalonia.Controls.TreeDataGrid, Avalonia.Controls.TreeDataGrid") is not null;
    }
#else
    public static bool IsTreeDataGridAvailable => false;
#endif
}