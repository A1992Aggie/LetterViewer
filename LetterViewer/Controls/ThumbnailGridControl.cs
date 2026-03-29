using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace LetterViewer.Controls;

[SupportedOSPlatform("windows")]
public class ThumbnailGridControl : UserControl
{
    private readonly FlowLayoutPanel _flowPanel;
    private readonly ConcurrentDictionary<string, Image> _thumbnailCache = new();
    private string[] _files = Array.Empty<string>();
    private int _thumbnailSize = 200;
    private string? _selectedFile;
    private CancellationTokenSource? _loadCts;

    public event Action<string>? FileSelected;
    public event Action<string>? FileDoubleClicked;
    public ThumbnailGridControl()
    {
        _flowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            BackColor = Color.FromArgb(30, 30, 30),
            Padding = new Padding(5)
        };

        _flowPanel.AllowDrop = true;
        _flowPanel.DragEnter += FlowPanel_DragEnter;
        _flowPanel.DragDrop += FlowPanel_DragDrop;

        Controls.Add(_flowPanel);
    }

    public string? SelectedFile => _selectedFile;

    public string[] SelectedFiles
    {
        get
        {
            var selected = new List<string>();
            foreach (Control ctrl in _flowPanel.Controls)
            {
                if (ctrl is ThumbnailItem item && item.IsSelected)
                    selected.Add(item.FilePath);
            }
            return selected.ToArray();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ThumbnailSize
    {
        get => _thumbnailSize;
        set
        {
            _thumbnailSize = Math.Clamp(value, 80, 500);
            RefreshThumbnails();
        }
    }

    public void LoadFiles(string[] files)
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        _files = files;

        _flowPanel.SuspendLayout();
        _flowPanel.Controls.Clear();

        foreach (var file in files)
        {
            var item = new ThumbnailItem(file, _thumbnailSize);
            item.Click += (s, e) => SelectItem(item, ModifierKeys.HasFlag(Keys.Control));
            item.DoubleClick += (s, e) => FileDoubleClicked?.Invoke(item.FilePath);
            item.MouseDown += ThumbnailItem_MouseDown;
            _flowPanel.Controls.Add(item);
        }

        _flowPanel.ResumeLayout();

        // Load thumbnails in background
        var ct = _loadCts.Token;
        Task.Run(() => LoadThumbnailsAsync(ct), ct);
    }

    private async Task LoadThumbnailsAsync(CancellationToken ct)
    {
        foreach (var file in _files)
        {
            if (ct.IsCancellationRequested) break;

            if (!_thumbnailCache.TryGetValue(file, out var thumb))
            {
                try
                {
                    thumb = await Task.Run(() => CreateThumbnail(file), ct);
                    _thumbnailCache[file] = thumb;
                }
                catch { continue; }
            }

            if (ct.IsCancellationRequested) break;

            try
            {
                Invoke(() =>
                {
                    foreach (Control ctrl in _flowPanel.Controls)
                    {
                        if (ctrl is ThumbnailItem item && item.FilePath == file)
                        {
                            item.SetThumbnail(thumb);
                            break;
                        }
                    }
                });
            }
            catch { break; }
        }
    }

    private Image CreateThumbnail(string filePath)
    {
        using var original = Image.FromFile(filePath);
        var oriented = Services.ImageProcessingService.ApplyExifOrientation(original);

        int size = _thumbnailSize - 20;
        double ratio = Math.Min((double)size / oriented.Width, (double)size / oriented.Height);
        int w = (int)(oriented.Width * ratio);
        int h = (int)(oriented.Height * ratio);

        var thumb = new Bitmap(w, h);
        using var g = Graphics.FromImage(thumb);
        Services.ImageProcessingService.SetHighQualityGraphics(g);
        g.DrawImage(oriented, 0, 0, w, h);

        if (oriented != original) oriented.Dispose();
        return thumb;
    }

    private void SelectItem(ThumbnailItem item, bool multiSelect)
    {
        if (!multiSelect)
        {
            foreach (Control ctrl in _flowPanel.Controls)
            {
                if (ctrl is ThumbnailItem ti)
                    ti.IsSelected = false;
            }
        }

        item.IsSelected = true;
        _selectedFile = item.FilePath;
        FileSelected?.Invoke(item.FilePath);
    }

    private void RefreshThumbnails()
    {
        _thumbnailCache.Clear();
        if (_files.Length > 0) LoadFiles(_files);
    }

    private void ThumbnailItem_MouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is ThumbnailItem item && e.Button == MouseButtons.Left)
        {
            var data = new DataObject(DataFormats.FileDrop, new[] { item.FilePath });
            DoDragDrop(data, DragDropEffects.Move);
        }
    }

    private void FlowPanel_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Move;
    }

    private void FlowPanel_DragDrop(object? sender, DragEventArgs e)
    {
        // Reorder is handled at form level
    }

    public void ClearCache()
    {
        foreach (var img in _thumbnailCache.Values) img.Dispose();
        _thumbnailCache.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loadCts?.Cancel();
            ClearCache();
        }
        base.Dispose(disposing);
    }
}

[SupportedOSPlatform("windows")]
public class ThumbnailItem : Panel
{
    private readonly PictureBox _pictureBox;
    private readonly Label _label;
    private bool _isSelected;

    public string FilePath { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            BackColor = value ? Color.FromArgb(60, 120, 200) : Color.FromArgb(45, 45, 45);
        }
    }

    public ThumbnailItem(string filePath, int size)
    {
        FilePath = filePath;
        Size = new Size(size, size + 20);
        BackColor = Color.FromArgb(45, 45, 45);
        Margin = new Padding(3);
        Cursor = Cursors.Hand;

        _pictureBox = new PictureBox
        {
            Size = new Size(size - 10, size - 10),
            Location = new Point(5, 5),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        _pictureBox.Click += (s, e) => OnClick(e);
        _pictureBox.DoubleClick += (s, e) => OnDoubleClick(e);
        _pictureBox.MouseDown += (s, e) => OnMouseDown(e);

        _label = new Label
        {
            Text = Path.GetFileName(filePath),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location = new Point(2, size - 4),
            Size = new Size(size - 4, 18),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 7.5f),
            AutoEllipsis = true
        };
        _label.Click += (s, e) => OnClick(e);
        _label.DoubleClick += (s, e) => OnDoubleClick(e);
        _label.MouseDown += (s, e) => OnMouseDown(e);

        Controls.Add(_pictureBox);
        Controls.Add(_label);
    }

    public void SetThumbnail(Image thumbnail)
    {
        _pictureBox.Image = thumbnail;
    }
}
