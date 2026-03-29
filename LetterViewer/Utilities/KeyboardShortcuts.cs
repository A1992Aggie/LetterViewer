namespace LetterViewer.Utilities;

public static class KeyboardShortcuts
{
    public static readonly Dictionary<Keys, string> ShortcutDescriptions = new()
    {
        { Keys.Left, "Previous image" },
        { Keys.Right, "Next image" },
        { Keys.Space, "Next image" },
        { Keys.Back, "Previous image" },
        { Keys.Oemplus, "Zoom in" },
        { Keys.OemMinus, "Zoom out" },
        { Keys.D0, "Fit to window" },
        { Keys.R, "Rotate 90° CW" },
        { Keys.D1, "Single view" },
        { Keys.D2, "Dual view" },
        { Keys.D3, "Thumbnail view" },
        { Keys.F2, "Rename file" },
        { Keys.Delete, "Delete (recycle bin)" },
        { Keys.I, "Toggle info overlay" },
        { Keys.C, "Crop mode" },
        { Keys.T, "OCR extract text" },
    };

    // Ctrl shortcuts described separately
    public static readonly Dictionary<Keys, string> CtrlShortcutDescriptions = new()
    {
        { Keys.Z, "Undo" },
        { Keys.B, "Brightness/Contrast" },
        { Keys.Oemplus, "Zoom in" },
        { Keys.OemMinus, "Zoom out" },
        { Keys.D0, "Fit to window" },
    };

    public static readonly Dictionary<Keys, string> ShiftShortcutDescriptions = new()
    {
        { Keys.R, "Rotate 90° CCW" },
    };
}
