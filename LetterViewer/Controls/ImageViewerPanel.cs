using System.Runtime.Versioning;
using LetterViewer.Models;

namespace LetterViewer.Controls;

[SupportedOSPlatform("windows")]
public class ImageViewerPanel : UserControl
{
    private ViewMode _viewMode = ViewMode.Single;
    private readonly Panel _singleViewPanel;
    private readonly PictureBox _singlePictureBox;
    private readonly SplitContainer _dualViewSplit;
    private readonly PictureBox _dualPictureBox1;
    private readonly PictureBox _dualPictureBox2;
    private readonly ThumbnailGridControl _thumbnailGrid;
    private readonly ImageInfoOverlay _infoOverlay;

    private Image? _currentImage;
    private string? _currentFile;
    private string[] _allFiles = Array.Empty<string>();
    private int _currentIndex = -1;
    private float _zoomLevel = 1.0f;
    private bool _isCropping;
    private Point _cropStart;
    private Rectangle _cropRect;
    private bool _isDragging;
    private Point _dragStart;
    private Point _scrollStart;

    public event Action<string>? FileSelected;
    public event Action<string>? FileDoubleClicked;

    public ViewMode CurrentViewMode => _viewMode;
    public string? CurrentFile => _currentFile;
    public int CurrentIndex => _currentIndex;
    public float ZoomLevel => _zoomLevel;
    public bool IsCropping => _isCropping;
    public Rectangle CropRectangle => _cropRect;
    public ThumbnailGridControl ThumbnailGrid => _thumbnailGrid;

    public ImageViewerPanel()
    {
        BackColor = Color.FromArgb(30, 30, 30);
        DoubleBuffered = true;

        // Single view
        _singleViewPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        _singlePictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30),
            Dock = DockStyle.Fill
        };
        _singlePictureBox.MouseWheel += SinglePictureBox_MouseWheel;
        _singlePictureBox.MouseDown += SinglePictureBox_MouseDown;
        _singlePictureBox.MouseMove += SinglePictureBox_MouseMove;
        _singlePictureBox.MouseUp += SinglePictureBox_MouseUp;
        _singlePictureBox.Paint += SinglePictureBox_Paint;

        _singleViewPanel.Controls.Add(_singlePictureBox);

        // Dual view
        _dualViewSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        _dualPictureBox1 = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        _dualPictureBox2 = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        _dualViewSplit.Panel1.Controls.Add(_dualPictureBox1);
        _dualViewSplit.Panel2.Controls.Add(_dualPictureBox2);

        // Thumbnail grid
        _thumbnailGrid = new ThumbnailGridControl { Dock = DockStyle.Fill };
        _thumbnailGrid.FileSelected += f => { _currentFile = f; FileSelected?.Invoke(f); };
        _thumbnailGrid.FileDoubleClicked += f =>
        {
            SetViewMode(ViewMode.Single);
            ShowFile(f);
            FileDoubleClicked?.Invoke(f);
        };

        // Info overlay
        _infoOverlay = new ImageInfoOverlay
        {
            Location = new Point(10, 10),
            OverlayVisible = false
        };

        _singleViewPanel.Controls.Add(_infoOverlay);
        _infoOverlay.BringToFront();

        // Default to single view
        Controls.Add(_singleViewPanel);
    }

    public void SetViewMode(ViewMode mode)
    {
        _viewMode = mode;

        Controls.Clear();
        switch (mode)
        {
            case ViewMode.Single:
                Controls.Add(_singleViewPanel);
                _singleViewPanel.Controls.Add(_infoOverlay);
                _infoOverlay.BringToFront();
                if (_currentFile != null) ShowFile(_currentFile);
                break;

            case ViewMode.Dual:
                Controls.Add(_dualViewSplit);
                ShowDualView();
                break;

            case ViewMode.ThumbnailGrid:
                Controls.Add(_thumbnailGrid);
                _thumbnailGrid.LoadFiles(_allFiles);
                break;
        }
    }

    public void SetFiles(string[] files)
    {
        _allFiles = files;
        if (_viewMode == ViewMode.ThumbnailGrid)
            _thumbnailGrid.LoadFiles(files);
    }

    public void ShowFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        _currentFile = filePath;
        _currentIndex = Array.IndexOf(_allFiles, filePath);

        try
        {
            _currentImage?.Dispose();
            _currentImage = LoadImageWithoutLock(filePath);
            _currentImage = Services.ImageProcessingService.ApplyExifOrientation(_currentImage);

            if (_viewMode == ViewMode.Single)
            {
                _singlePictureBox.Image?.Dispose();
                _singlePictureBox.Image = _currentImage;
                ApplyZoom();
                UpdateInfoOverlay();
            }
            else if (_viewMode == ViewMode.Dual)
            {
                ShowDualView();
            }

            FileSelected?.Invoke(filePath);
        }
        catch (Exception ex)
        {
            _singlePictureBox.Image = null;
            MessageBox.Show($"Cannot load image: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ShowNext()
    {
        if (_allFiles.Length == 0) return;

        int step = _viewMode == ViewMode.Dual ? 2 : 1;
        int next = _currentIndex + step;
        if (next >= _allFiles.Length) next = 0;

        ShowFile(_allFiles[next]);
    }

    public void ShowPrevious()
    {
        if (_allFiles.Length == 0) return;

        int step = _viewMode == ViewMode.Dual ? 2 : 1;
        int prev = _currentIndex - step;
        if (prev < 0) prev = _allFiles.Length - 1;

        ShowFile(_allFiles[prev]);
    }

    public void ZoomIn()
    {
        _zoomLevel = Math.Min(_zoomLevel * 1.25f, 10.0f);
        ApplyZoom();
        UpdateInfoOverlay();
    }

    public void ZoomOut()
    {
        _zoomLevel = Math.Max(_zoomLevel / 1.25f, 0.1f);
        ApplyZoom();
        UpdateInfoOverlay();
    }

    public void ZoomFit()
    {
        _zoomLevel = 1.0f;
        ApplyZoom();
        UpdateInfoOverlay();
    }

    public void ToggleInfoOverlay()
    {
        _infoOverlay.OverlayVisible = !_infoOverlay.OverlayVisible;
    }

    public void StartCropMode()
    {
        _isCropping = true;
        _cropRect = Rectangle.Empty;
        _singlePictureBox.Cursor = Cursors.Cross;
    }

    public void CancelCrop()
    {
        _isCropping = false;
        _cropRect = Rectangle.Empty;
        _singlePictureBox.Cursor = Cursors.Default;
        _singlePictureBox.Invalidate();
    }

    public Rectangle GetCropRectInImageCoords()
    {
        if (_currentImage == null || _cropRect.IsEmpty) return Rectangle.Empty;

        // Convert from PictureBox coordinates to image coordinates
        var imgRect = GetImageDisplayRect();
        if (imgRect.Width <= 0 || imgRect.Height <= 0) return Rectangle.Empty;

        float scaleX = (float)_currentImage.Width / imgRect.Width;
        float scaleY = (float)_currentImage.Height / imgRect.Height;

        int x = (int)((_cropRect.X - imgRect.X) * scaleX);
        int y = (int)((_cropRect.Y - imgRect.Y) * scaleY);
        int w = (int)(_cropRect.Width * scaleX);
        int h = (int)(_cropRect.Height * scaleY);

        return new Rectangle(
            Math.Max(0, x), Math.Max(0, y),
            Math.Min(w, _currentImage.Width - x),
            Math.Min(h, _currentImage.Height - y));
    }

    public void ApplyCrop()
    {
        if (_currentImage == null || _cropRect.IsEmpty || _currentFile == null) return;

        var cropRect = GetCropRectInImageCoords();
        if (cropRect.Width <= 0 || cropRect.Height <= 0) return;

        using var original = new Bitmap(_currentImage);
        var cropped = Services.ImageProcessingService.Crop(original, cropRect);
        Services.ImageProcessingService.SaveWithMaxQuality(cropped, _currentFile);

        CancelCrop();
        ShowFile(_currentFile);
    }

    public void RotateCurrentImage(int degrees)
    {
        if (_currentImage == null || _currentFile == null) return;

        using var original = new Bitmap(_currentImage);
        Bitmap rotated = degrees switch
        {
            90 => Services.ImageProcessingService.Rotate90CW(original),
            -90 or 270 => Services.ImageProcessingService.Rotate90CCW(original),
            180 => Services.ImageProcessingService.Rotate180(original),
            _ => new Bitmap(original)
        };

        Services.ImageProcessingService.SaveWithMaxQuality(rotated, _currentFile);
        rotated.Dispose();
        ShowFile(_currentFile);
    }

    public void ApplyBrightnessContrast(float brightness, float contrast)
    {
        if (_currentImage == null || _currentFile == null) return;

        using var original = new Bitmap(_currentImage);
        var adjusted = Services.ImageProcessingService.AdjustBrightnessContrast(original, brightness, contrast);
        Services.ImageProcessingService.SaveWithMaxQuality(adjusted, _currentFile);
        adjusted.Dispose();
        ShowFile(_currentFile);
    }

    private void ApplyZoom()
    {
        if (_currentImage == null) return;

        if (Math.Abs(_zoomLevel - 1.0f) < 0.01f)
        {
            // Fit mode
            _singlePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _singlePictureBox.Dock = DockStyle.Fill;
        }
        else
        {
            // Zoom mode - use actual pixel sizes
            _singlePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _singlePictureBox.Dock = DockStyle.None;

            int w = (int)(_currentImage.Width * _zoomLevel);
            int h = (int)(_currentImage.Height * _zoomLevel);
            _singlePictureBox.Size = new Size(w, h);

            // Center if smaller than panel
            int x = Math.Max(0, (_singleViewPanel.ClientSize.Width - w) / 2);
            int y = Math.Max(0, (_singleViewPanel.ClientSize.Height - h) / 2);
            _singlePictureBox.Location = new Point(x, y);
        }
    }

    private void ShowDualView()
    {
        if (_currentIndex < 0 || _allFiles.Length == 0) return;

        try
        {
            _dualPictureBox1.Image?.Dispose();
            var img1 = LoadImageWithoutLock(_allFiles[_currentIndex]);
            _dualPictureBox1.Image = Services.ImageProcessingService.ApplyExifOrientation(img1);

            int nextIndex = _currentIndex + 1;
            if (nextIndex < _allFiles.Length)
            {
                _dualPictureBox2.Image?.Dispose();
                var img2 = LoadImageWithoutLock(_allFiles[nextIndex]);
                _dualPictureBox2.Image = Services.ImageProcessingService.ApplyExifOrientation(img2);
            }
            else
            {
                _dualPictureBox2.Image = null;
            }
        }
        catch { }
    }

    private void UpdateInfoOverlay()
    {
        if (_currentFile != null && _infoOverlay.OverlayVisible)
        {
            _infoOverlay.UpdateInfo(_currentFile, _zoomLevel);
        }
    }

    private Rectangle GetImageDisplayRect()
    {
        if (_singlePictureBox.Image == null) return Rectangle.Empty;

        var img = _singlePictureBox.Image;
        var pb = _singlePictureBox;

        float ratioX = (float)pb.Width / img.Width;
        float ratioY = (float)pb.Height / img.Height;
        float ratio = Math.Min(ratioX, ratioY);

        int displayW = (int)(img.Width * ratio);
        int displayH = (int)(img.Height * ratio);
        int x = (pb.Width - displayW) / 2;
        int y = (pb.Height - displayH) / 2;

        return new Rectangle(x, y, displayW, displayH);
    }

    // Mouse event handlers
    private void SinglePictureBox_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (e.Delta > 0) ZoomIn();
        else ZoomOut();
    }

    private void SinglePictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_isCropping && e.Button == MouseButtons.Left)
        {
            _cropStart = e.Location;
            _cropRect = new Rectangle(e.Location, Size.Empty);
        }
        else if (e.Button == MouseButtons.Left && !_isCropping)
        {
            _isDragging = true;
            _dragStart = e.Location;
            _scrollStart = _singleViewPanel.AutoScrollPosition;
            _singlePictureBox.Cursor = Cursors.SizeAll;
        }
    }

    private void SinglePictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isCropping && e.Button == MouseButtons.Left)
        {
            int x = Math.Min(_cropStart.X, e.X);
            int y = Math.Min(_cropStart.Y, e.Y);
            int w = Math.Abs(e.X - _cropStart.X);
            int h = Math.Abs(e.Y - _cropStart.Y);
            _cropRect = new Rectangle(x, y, w, h);
            _singlePictureBox.Invalidate();
        }
        else if (_isDragging && e.Button == MouseButtons.Left)
        {
            int dx = e.X - _dragStart.X;
            int dy = e.Y - _dragStart.Y;
            _singleViewPanel.AutoScrollPosition = new Point(
                -_scrollStart.X - dx,
                -_scrollStart.Y - dy);
        }
    }

    private void SinglePictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _singlePictureBox.Cursor = _isCropping ? Cursors.Cross : Cursors.Default;
        }
    }

    private void SinglePictureBox_Paint(object? sender, PaintEventArgs e)
    {
        if (_isCropping && !_cropRect.IsEmpty)
        {
            using var pen = new Pen(Color.Red, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, _cropRect);

            // Dim area outside crop
            using var dimBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
            var fullRect = _singlePictureBox.ClientRectangle;

            // Top
            e.Graphics.FillRectangle(dimBrush, 0, 0, fullRect.Width, _cropRect.Top);
            // Bottom
            e.Graphics.FillRectangle(dimBrush, 0, _cropRect.Bottom, fullRect.Width, fullRect.Height - _cropRect.Bottom);
            // Left
            e.Graphics.FillRectangle(dimBrush, 0, _cropRect.Top, _cropRect.Left, _cropRect.Height);
            // Right
            e.Graphics.FillRectangle(dimBrush, _cropRect.Right, _cropRect.Top, fullRect.Width - _cropRect.Right, _cropRect.Height);
        }
    }

    private static Image LoadImageWithoutLock(string filePath)
    {
        using var ms = new MemoryStream(File.ReadAllBytes(filePath));
        using var tmp = Image.FromStream(ms, false, false);
        var bmp = new Bitmap(tmp);
        foreach (var propItem in tmp.PropertyItems)
            bmp.SetPropertyItem(propItem);
        return bmp;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _currentImage?.Dispose();
            _singlePictureBox.Image?.Dispose();
            _dualPictureBox1.Image?.Dispose();
            _dualPictureBox2.Image?.Dispose();
            _thumbnailGrid.Dispose();
        }
        base.Dispose(disposing);
    }
}
