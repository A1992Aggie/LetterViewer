using System.Runtime.Versioning;
using LetterViewer.Controls;
using LetterViewer.Dialogs;
using LetterViewer.Models;
using LetterViewer.Services;
using LetterViewer.Utilities;

namespace LetterViewer;

[SupportedOSPlatform("windows")]
public class MainForm : Form
{
    private readonly SplitContainer _mainSplit;
    private readonly DirectoryBrowserPanel _directoryPanel;
    private readonly ImageViewerPanel _imageViewer;
    private readonly ToolStrip _toolStrip;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _undoStatusLabel;
    private readonly UndoService _undoService;
    private readonly FileOperationService _fileService;

    public MainForm()
    {
        _undoService = new UndoService();
        _fileService = new FileOperationService(_undoService);

        InitializeForm();

        // Main split container
        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 45),
            BorderStyle = BorderStyle.None
        };

        // Left panel - directory browser
        _directoryPanel = new DirectoryBrowserPanel { Dock = DockStyle.Fill };
        _directoryPanel.FileSelected += OnFileSelected;
        _directoryPanel.DirectoryChanged += OnDirectoryChanged;
        _directoryPanel.RenameRequested += DoRename;
        _directoryPanel.MoveRequested += DoMove;
        _directoryPanel.DeleteRequested += DoDelete;
        _directoryPanel.FilesDroppedOnDestination += OnFilesDroppedOnDestination;

        // Right panel - image viewer
        _imageViewer = new ImageViewerPanel { Dock = DockStyle.Fill };

        _mainSplit.Panel1.Controls.Add(_directoryPanel);
        _mainSplit.Panel2.Controls.Add(_imageViewer);

        // Toolbar
        _toolStrip = CreateToolStrip();

        // Status bar
        _statusStrip = new StatusStrip
        {
            BackColor = Color.FromArgb(0, 122, 204)
        };
        _statusLabel = new ToolStripStatusLabel("Ready")
        {
            ForeColor = Color.White,
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _undoStatusLabel = new ToolStripStatusLabel("")
        {
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight
        };
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_undoStatusLabel);

        Controls.Add(_mainSplit);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);

        // Wire undo service
        _undoService.UndoStackChanged += () =>
        {
            _undoStatusLabel.Text = _undoService.CanUndo
                ? $"Undo: {_undoService.LastActionDescription} (Ctrl+Z)"
                : "";
        };

        // Set splitter after form is shown
        Shown += (s, e) =>
        {
            SetupMultiMonitor();
        };

        // Enable drag-drop on tree
        AllowDrop = true;
        DragEnter += (s, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Move;
        };

        KeyPreview = true;
    }

    private void InitializeForm()
    {
        Text = "Letter Viewer";
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9f);
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.Manual;
    }

    private void SetupMultiMonitor()
    {
        var screens = Screen.AllScreens.OrderBy(s => s.Bounds.X).ToArray();
        if (screens.Length >= 2)
        {
            var bounds = Rectangle.Union(screens[0].WorkingArea, screens[1].WorkingArea);
            Location = bounds.Location;
            Size = bounds.Size;
            WindowState = FormWindowState.Normal;

            // Left panel = first monitor width
            _mainSplit.SplitterDistance = screens[0].WorkingArea.Width;
        }
        else
        {
            // Single monitor - use 30% for left panel
            _mainSplit.SplitterDistance = (int)(ClientSize.Width * 0.30);
        }
    }

    private ToolStrip CreateToolStrip()
    {
        var strip = new ToolStrip
        {
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            GripStyle = ToolStripGripStyle.Hidden,
            Renderer = new DarkToolStripRenderer(),
            AutoSize = false,
            Height = 40
        };

        // View mode buttons
        strip.Items.Add(CreateButton("Single", "1 - Single view", (s, e) => SetViewMode(ViewMode.Single)));
        strip.Items.Add(CreateButton("Dual", "2 - Dual view", (s, e) => SetViewMode(ViewMode.Dual)));
        strip.Items.Add(CreateButton("Grid", "3 - Thumbnail grid", (s, e) => SetViewMode(ViewMode.ThumbnailGrid)));
        strip.Items.Add(new ToolStripSeparator());

        // Zoom
        strip.Items.Add(CreateButton("Zoom +", "Zoom in (+)", (s, e) => _imageViewer.ZoomIn()));
        strip.Items.Add(CreateButton("Zoom -", "Zoom out (-)", (s, e) => _imageViewer.ZoomOut()));
        strip.Items.Add(CreateButton("Fit", "Fit to window (0)", (s, e) => _imageViewer.ZoomFit()));
        strip.Items.Add(new ToolStripSeparator());

        // Rotation
        strip.Items.Add(CreateButton("Rot CW", "Rotate 90° clockwise (R)", (s, e) => _imageViewer.RotateCurrentImage(90)));
        strip.Items.Add(CreateButton("Rot CCW", "Rotate 90° counter-clockwise (Shift+R)", (s, e) => _imageViewer.RotateCurrentImage(-90)));
        strip.Items.Add(CreateButton("Rot 180", "Rotate 180°", (s, e) => _imageViewer.RotateCurrentImage(180)));
        strip.Items.Add(new ToolStripSeparator());

        // Image operations
        strip.Items.Add(CreateButton("Crop", "Crop tool (C)", (s, e) => StartCrop()));
        strip.Items.Add(CreateButton("B/C", "Brightness/Contrast (Ctrl+B)", (s, e) => DoBrightnessContrast()));
        strip.Items.Add(CreateButton("Combine", "Combine images", (s, e) => DoCombine()));
        strip.Items.Add(CreateButton("Img+Txt", "Combine image with text file", (s, e) => DoImageTextCombine()));
        strip.Items.Add(CreateButton("OCR", "Extract text (T)", (s, e) => DoOcr()));
        strip.Items.Add(new ToolStripSeparator());

        // File operations
        strip.Items.Add(CreateButton("Rename", "Rename file (F2)", (s, e) => DoRename()));
        strip.Items.Add(CreateButton("Batch", "Batch rename", (s, e) => DoBatchRename()));
        strip.Items.Add(CreateButton("Move", "Move to folder", (s, e) => DoMove()));
        strip.Items.Add(CreateButton("Delete", "Delete to Recycle Bin (Del)", (s, e) => DoDelete()));
        strip.Items.Add(new ToolStripSeparator());

        // Info and undo
        strip.Items.Add(CreateButton("Info", "Toggle info overlay (I)", (s, e) => _imageViewer.ToggleInfoOverlay()));
        strip.Items.Add(CreateButton("Undo", "Undo last action (Ctrl+Z)", (s, e) => DoUndo()));
        strip.Items.Add(new ToolStripSeparator());

        // Refresh
        strip.Items.Add(CreateButton("Refresh All", "Refresh folders and files (F5)", (s, e) => _directoryPanel.RefreshAll()));

        return strip;
    }

    private ToolStripButton CreateButton(string text, string tooltip, EventHandler click)
    {
        var btn = new ToolStripButton(text)
        {
            ToolTipText = tooltip,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f),
            Padding = new Padding(10, 4, 10, 4)
        };
        btn.Click += click;
        return btn;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F5:
                _directoryPanel.RefreshAll();
                _statusLabel.Text = "Refreshed";
                return true;

            case Keys.Left:
            case Keys.Back:
                _imageViewer.ShowPrevious();
                SyncSelection();
                return true;

            case Keys.Right:
            case Keys.Space:
                _imageViewer.ShowNext();
                SyncSelection();
                return true;

            case Keys.Oemplus:
            case Keys.Add:
                _imageViewer.ZoomIn();
                return true;

            case Keys.OemMinus:
            case Keys.Subtract:
                _imageViewer.ZoomOut();
                return true;

            case Keys.D0:
            case Keys.NumPad0:
                _imageViewer.ZoomFit();
                return true;

            case Keys.Control | Keys.Oemplus:
            case Keys.Control | Keys.Add:
                _imageViewer.ZoomIn();
                return true;

            case Keys.Control | Keys.OemMinus:
            case Keys.Control | Keys.Subtract:
                _imageViewer.ZoomOut();
                return true;

            case Keys.Control | Keys.D0:
                _imageViewer.ZoomFit();
                return true;

            case Keys.R:
                _imageViewer.RotateCurrentImage(90);
                return true;

            case Keys.Shift | Keys.R:
                _imageViewer.RotateCurrentImage(-90);
                return true;

            case Keys.D1:
                SetViewMode(ViewMode.Single);
                return true;

            case Keys.D2:
                SetViewMode(ViewMode.Dual);
                return true;

            case Keys.D3:
                SetViewMode(ViewMode.ThumbnailGrid);
                return true;

            case Keys.F2:
                DoRename();
                return true;

            case Keys.Delete:
                DoDelete();
                return true;

            case Keys.Control | Keys.Z:
                DoUndo();
                return true;

            case Keys.I:
                _imageViewer.ToggleInfoOverlay();
                return true;

            case Keys.C:
                StartCrop();
                return true;

            case Keys.T:
                DoOcr();
                return true;

            case Keys.Control | Keys.B:
                DoBrightnessContrast();
                return true;

            case Keys.Escape:
                if (_imageViewer.IsCropping)
                {
                    _imageViewer.CancelCrop();
                    _statusLabel.Text = "Crop cancelled";
                    return true;
                }
                break;

            case Keys.Enter:
                if (_imageViewer.IsCropping)
                {
                    _imageViewer.ApplyCrop();
                    _statusLabel.Text = "Crop applied";
                    RefreshFiles();
                    return true;
                }
                break;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void OnFileSelected(string filePath)
    {
        _imageViewer.ShowFile(filePath);
        _statusLabel.Text = $"{Path.GetFileName(filePath)} | {_imageViewer.CurrentIndex + 1} of {_directoryPanel.GetAllFiles().Length}";
    }

    private void OnDirectoryChanged(string directory)
    {
        var files = _directoryPanel.GetAllFiles();
        _imageViewer.SetFiles(files);
        _statusLabel.Text = $"{directory} | {files.Length} images";
    }

    private void SetViewMode(ViewMode mode)
    {
        _imageViewer.SetViewMode(mode);
        _statusLabel.Text = $"View: {mode}";
    }

    private void SyncSelection()
    {
        if (_imageViewer.CurrentFile != null)
        {
            _directoryPanel.SelectFileByPath(_imageViewer.CurrentFile);
            _statusLabel.Text = $"{Path.GetFileName(_imageViewer.CurrentFile)} | {_imageViewer.CurrentIndex + 1} of {_directoryPanel.GetAllFiles().Length}";
        }
    }

    private void OnFilesDroppedOnDestination(string[] files, string destPath)
    {
        int moved = 0;
        foreach (var file in files)
        {
            try
            {
                _fileService.MoveFile(file, destPath);
                moved++;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving {Path.GetFileName(file)}: {ex.Message}",
                    "Move Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        if (moved > 0)
        {
            MoveFileDialog.AddRecentDirectory(destPath);
            _statusLabel.Text = $"Moved {moved} file(s) to {destPath}";
            RefreshFiles();
        }
    }

    private void DoRename()
    {
        var files = _directoryPanel.GetSelectedFiles();
        if (files.Length == 0 && _imageViewer.CurrentFile != null)
            files = new[] { _imageViewer.CurrentFile };
        if (files.Length != 1) return;

        string oldName = Path.GetFileName(files[0]);
        string? newName = ShowInputDialog("Rename File", "New name:", oldName);
        if (newName == null || newName == oldName) return;

        try
        {
            string newPath = _fileService.RenameFile(files[0], newName);
            _statusLabel.Text = $"Renamed to {newName}";
            RefreshFiles();
            _directoryPanel.SelectFileByPath(newPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Rename Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DoMove()
    {
        var files = _directoryPanel.GetSelectedFiles();
        if (files.Length == 0 && _imageViewer.CurrentFile != null)
            files = new[] { _imageViewer.CurrentFile };
        if (files.Length == 0) return;

        using var dlg = new MoveFileDialog(files, _directoryPanel.CurrentDirectory);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string dest = dlg.DestinationDirectory;
        if (!Directory.Exists(dest))
        {
            var result = MessageBox.Show($"Create directory '{dest}'?", "Create Directory",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
        }

        int moved = 0;
        foreach (var file in files)
        {
            try
            {
                _fileService.MoveFile(file, dest);
                moved++;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving {Path.GetFileName(file)}: {ex.Message}",
                    "Move Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        MoveFileDialog.AddRecentDirectory(dest);
        _statusLabel.Text = $"Moved {moved} file(s) to {dest}";
        RefreshFiles();
    }

    private void DoDelete()
    {
        var files = _directoryPanel.GetSelectedFiles();
        if (files.Length == 0 && _imageViewer.CurrentFile != null)
            files = new[] { _imageViewer.CurrentFile };
        if (files.Length == 0) return;

        string msg = files.Length == 1
            ? $"Move '{Path.GetFileName(files[0])}' to Recycle Bin?"
            : $"Move {files.Length} files to Recycle Bin?";

        if (MessageBox.Show(msg, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        foreach (var file in files)
        {
            try
            {
                _fileService.MoveToRecycleBin(file);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Delete Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        _statusLabel.Text = $"Deleted {files.Length} file(s) to Recycle Bin";
        RefreshFiles();
    }

    private void DoBatchRename()
    {
        var files = _directoryPanel.GetSelectedFiles();
        if (files.Length < 2)
            files = _directoryPanel.GetAllFiles();
        if (files.Length == 0) return;

        using var dlg = new BatchRenameDialog(files);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var renames = _fileService.BatchRename(files, dlg.Pattern);
            _statusLabel.Text = $"Renamed {renames.Count} file(s)";
            RefreshFiles();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Batch Rename Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DoCombine()
    {
        var files = _directoryPanel.GetSelectedFiles();
        if (files.Length < 2)
        {
            MessageBox.Show("Select at least 2 images to combine.", "Combine Images",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new CombineImagesDialog(files);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string outputPath = Path.Combine(_directoryPanel.CurrentDirectory, dlg.OutputFileName);
            ImageCombiner.CombineImages(files, outputPath, dlg.Direction, dlg.Spacing, dlg.NormalizeSize);
            _statusLabel.Text = $"Combined {files.Length} images → {dlg.OutputFileName}";
            RefreshFiles();
            _directoryPanel.SelectFileByPath(outputPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Combine Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DoImageTextCombine()
    {
        if (_imageViewer.CurrentFile == null)
        {
            _statusLabel.Text = "Select an image first";
            return;
        }

        using var dlg = new CombineImageTextDialog(_imageViewer.CurrentFile);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string outputPath = Path.Combine(
                Path.GetDirectoryName(_imageViewer.CurrentFile)!,
                dlg.OutputFileName);
            TextToBitmapWithFirstImage.CreateBitmapFromText(_imageViewer.CurrentFile, dlg.PastedText, outputPath);
            _statusLabel.Text = $"Created {dlg.OutputFileName}";
            RefreshFiles();
            _directoryPanel.SelectFileByPath(outputPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Image+Text Combine Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DoBrightnessContrast()
    {
        if (_imageViewer.CurrentFile == null) return;

        using var img = Image.FromFile(_imageViewer.CurrentFile);
        using var dlg = new BrightnessContrastDialog(img);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _imageViewer.ApplyBrightnessContrast(dlg.Brightness, dlg.Contrast);
            _statusLabel.Text = "Brightness/Contrast applied";
        }
    }

    private async void DoOcr()
    {
        if (_imageViewer.CurrentFile == null) return;

        _statusLabel.Text = "Running OCR...";
        string text = await OcrService.ExtractTextAsync(_imageViewer.CurrentFile);

        using var dlg = new OcrResultDialog(text, Path.GetFileName(_imageViewer.CurrentFile));
        dlg.ShowDialog(this);
        _statusLabel.Text = "Ready";

        if (dlg.CombineRequested)
        {
            string imageFile = _imageViewer.CurrentFile;
            string baseName = Path.GetFileNameWithoutExtension(imageFile);
            string outputPath = Path.Combine(Path.GetDirectoryName(imageFile)!, $"ocr_{baseName}.jpg");
            try
            {
                TextToBitmapWithFirstImage.CreateBitmapFromText(imageFile, dlg.OcrText, outputPath);
                _statusLabel.Text = $"Created ocr_{baseName}.jpg";
                RefreshFiles();
                _directoryPanel.SelectFileByPath(outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Image+Text Combine Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void DoUndo()
    {
        if (_undoService.CanUndo)
        {
            string? desc = _undoService.LastActionDescription;
            if (_undoService.Undo())
            {
                _statusLabel.Text = $"Undone: {desc}";
                RefreshFiles();
            }
        }
        else
        {
            _statusLabel.Text = "Nothing to undo";
        }
    }

    private void StartCrop()
    {
        if (_imageViewer.CurrentViewMode != ViewMode.Single) return;
        _imageViewer.StartCropMode();
        _statusLabel.Text = "Crop: drag to select, Enter to apply, Escape to cancel";
    }

    private void RefreshFiles()
    {
        _directoryPanel.RefreshFileList();
        var files = _directoryPanel.GetAllFiles();
        _imageViewer.SetFiles(files);
    }

    private string? ShowInputDialog(string title, string prompt, string defaultValue)
    {
        using var dlg = new Form
        {
            Text = title,
            Size = new Size(400, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(37, 37, 38)
        };

        var lbl = new Label
        {
            Text = prompt,
            Location = new Point(15, 15),
            ForeColor = Color.White,
            AutoSize = true
        };

        var txt = new TextBox
        {
            Text = defaultValue,
            Location = new Point(15, 40),
            Size = new Size(355, 25),
            BackColor = Color.FromArgb(51, 51, 51),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        // Select just the name part, not extension
        int dotIndex = defaultValue.LastIndexOf('.');
        if (dotIndex > 0)
        {
            txt.SelectionStart = 0;
            txt.SelectionLength = dotIndex;
        }

        var okBtn = new Button
        {
            Text = "OK",
            Location = new Point(210, 80),
            Size = new Size(75, 30),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(295, 80),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };

        dlg.Controls.AddRange(new Control[] { lbl, txt, okBtn, cancelBtn });
        dlg.AcceptButton = okBtn;
        dlg.CancelButton = cancelBtn;

        return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
    }
}

public class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.White;
        base.OnRenderItemText(e);
    }
}

public class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => Color.FromArgb(62, 62, 64);
    public override Color MenuItemBorder => Color.FromArgb(62, 62, 64);
    public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
    public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 48);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 48);
    public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 48);
    public override Color SeparatorDark => Color.FromArgb(62, 62, 64);
    public override Color SeparatorLight => Color.FromArgb(62, 62, 64);
    public override Color MenuBorder => Color.FromArgb(62, 62, 64);
    public override Color ButtonSelectedHighlight => Color.FromArgb(62, 62, 64);
    public override Color ButtonSelectedHighlightBorder => Color.FromArgb(62, 62, 64);
    public override Color ButtonPressedHighlight => Color.FromArgb(0, 122, 204);
    public override Color ButtonPressedHighlightBorder => Color.FromArgb(0, 122, 204);
    public override Color ButtonCheckedHighlight => Color.FromArgb(0, 122, 204);
    public override Color ButtonCheckedHighlightBorder => Color.FromArgb(0, 122, 204);
    public override Color ButtonSelectedBorder => Color.FromArgb(62, 62, 64);
    public override Color ButtonSelectedGradientBegin => Color.FromArgb(62, 62, 64);
    public override Color ButtonSelectedGradientEnd => Color.FromArgb(62, 62, 64);
    public override Color ButtonPressedGradientBegin => Color.FromArgb(0, 122, 204);
    public override Color ButtonPressedGradientEnd => Color.FromArgb(0, 122, 204);
    public override Color OverflowButtonGradientBegin => Color.FromArgb(45, 45, 48);
    public override Color OverflowButtonGradientMiddle => Color.FromArgb(45, 45, 48);
    public override Color OverflowButtonGradientEnd => Color.FromArgb(45, 45, 48);
}
