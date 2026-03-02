namespace Devolutions.AvaloniaControls.Helpers;

public static class AvaloniaAccelerateHelpers
{
#if ENABLE_ACCELERATE
    private static bool? isTreeDataGridAvailable;
    public static bool IsTreeDataGridAvailable
    {
        get
        {
            isTreeDataGridAvailable ??= Type.GetType("Avalonia.Controls.TreeDataGrid, Avalonia.Controls.TreeDataGrid") is not null;
            return (bool)isTreeDataGridAvailable;
        }
    }
#else
    public static bool IsTreeDataGridAvailable => false;
#endif
}