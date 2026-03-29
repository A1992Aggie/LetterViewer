using System.ComponentModel;
using System.Runtime.Versioning;

namespace LetterViewer.Controls;

[SupportedOSPlatform("windows")]
public class ImageInfoOverlay : UserControl
{
    private Dictionary<string, string> _info = new();
    private bool _visible = true;

    public ImageInfoOverlay()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        DoubleBuffered = true;
        Size = new Size(280, 160);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool OverlayVisible
    {
        get => _visible;
        set { _visible = value; Invalidate(); }
    }

    public void UpdateInfo(Dictionary<string, string> info)
    {
        _info = info;
        Invalidate();
    }

    public void UpdateInfo(string filePath, float zoomLevel)
    {
        _info = Utilities.ExifHelper.GetImageInfo(filePath);
        _info["Zoom"] = $"{zoomLevel * 100:F0}%";
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!_visible || _info.Count == 0) return;

        var g = e.Graphics;

        using var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        using var textBrush = new SolidBrush(Color.White);
        using var labelBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
        using var font = new Font("Segoe UI", 9f);
        using var boldFont = new Font("Segoe UI", 9f, FontStyle.Bold);

        int padding = 8;
        int lineHeight = 18;
        int totalHeight = padding * 2 + _info.Count * lineHeight;
        int maxWidth = 0;

        foreach (var kvp in _info)
        {
            var labelSize = g.MeasureString(kvp.Key + ": ", boldFont);
            var valueSize = g.MeasureString(kvp.Value, font);
            maxWidth = Math.Max(maxWidth, (int)(labelSize.Width + valueSize.Width));
        }

        int boxWidth = maxWidth + padding * 2;
        int boxHeight = totalHeight;
        Size = new Size(boxWidth + 4, boxHeight + 4);

        g.FillRoundedRectangle(bgBrush, new Rectangle(0, 0, boxWidth, boxHeight), 6);

        int y = padding;
        foreach (var kvp in _info)
        {
            var labelSize = g.MeasureString(kvp.Key + ": ", boldFont);
            g.DrawString(kvp.Key + ": ", boldFont, labelBrush, padding, y);
            g.DrawString(kvp.Value, font, textBrush, padding + labelSize.Width, y);
            y += lineHeight;
        }
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
