using System.Runtime.Versioning;
using LetterViewer.Models;

namespace LetterViewer.Controls;

[SupportedOSPlatform("windows")]
public class DirectoryBrowserPanel : UserControl
{
    private readonly TreeView _treeView;
    private readonly TreeView _destinationTreeView;
    private readonly ListView _fileListView;
    private readonly SplitContainer _splitContainer;
    private readonly SplitContainer _topSplit;
    private readonly TextBox _pathTextBox;
    private readonly Button _browseButton;
    private readonly ImageList _thumbnailList;
    private string _currentDirectory = Directory.Exists(@"C:\Users\evan\OneDrive\Pictures\NonDisplay\Fisher\NotProcessed")
        ? @"C:\Users\evan\OneDrive\Pictures\NonDisplay\Fisher\NotProcessed"
        : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    private SortMode _sortMode = SortMode.Name;
    private bool _sortAscending = true;
    private bool _suppressNavigate;

    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".webp" };

    public event Action<string>? FileSelected;
    public event Action<string[]>? FilesSelected;
    public event Action<string>? DirectoryChanged;

    public string CurrentDirectory => _currentDirectory;

    public DirectoryBrowserPanel()
    {
        BackColor = Color.FromArgb(37, 37, 38);

        // Path bar
        var pathPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(2)
        };

        _pathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = _currentDirectory,
            BackColor = Color.FromArgb(51, 51, 51),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _pathTextBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                NavigateToDirectory(_pathTextBox.Text);
                e.SuppressKeyPress = true;
            }
        };

        _browseButton = new Button
        {
            Dock = DockStyle.Right,
            Width = 30,
            Text = "...",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        _browseButton.Click += BrowseButton_Click;

        pathPanel.Controls.Add(_pathTextBox);
        pathPanel.Controls.Add(_browseButton);

        // Split container for tree and file list
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 250,
            BackColor = Color.FromArgb(37, 37, 38)
        };

        // Tree view for directories
        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            ShowLines = true,
            ShowPlusMinus = true,
            HideSelection = false,
            Font = new Font("Segoe UI", 12f)
        };
        _treeView.AfterSelect += TreeView_AfterSelect;
        _treeView.BeforeExpand += TreeView_BeforeExpand;

        var treeContextMenu = new ContextMenuStrip();
        treeContextMenu.Items.Add("Refresh", null, (s, e) => RefreshAll());
        _treeView.ContextMenuStrip = treeContextMenu;

        _thumbnailList = new ImageList
        {
            ImageSize = new Size(160, 120),
            ColorDepth = ColorDepth.Depth32Bit
        };

        // File list view
        _fileListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.LargeIcon,
            FullRowSelect = true,
            MultiSelect = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9f),
            LargeImageList = _thumbnailList
        };

        _fileListView.Columns.Add("Name", 200);
        _fileListView.Columns.Add("Date", 120);
        _fileListView.Columns.Add("Size", 80);
        _fileListView.Columns.Add("Dimensions", 100);

        _fileListView.SelectedIndexChanged += FileListView_SelectedIndexChanged;
        _fileListView.ColumnClick += FileListView_ColumnClick;
        _fileListView.AllowDrop = true;
        _fileListView.ItemDrag += FileListView_ItemDrag;

        // Context menu for file list
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Rename (F2)", null, (s, e) => RenameSelected());
        contextMenu.Items.Add("Move to...", null, (s, e) => MoveSelected());
        contextMenu.Items.Add("Delete (Recycle Bin)", null, (s, e) => DeleteSelected());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Copy Path", null, (s, e) => CopyPathSelected());
        contextMenu.Items.Add("Open in Explorer", null, (s, e) => OpenInExplorer());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Refresh", null, (s, e) => RefreshAll());
        _fileListView.ContextMenuStrip = contextMenu;

        // Top split — source browser (left) + move-target browser (right)
        _topSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = Color.FromArgb(37, 37, 38)
        };

        var sourceHeader = new Label
        {
            Text = "Browse",
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(150, 150, 150),
            BackColor = Color.FromArgb(45, 45, 48),
            Font = new Font("Segoe UI", 10f),
            Padding = new Padding(4, 2, 0, 0)
        };
        _topSplit.Panel1.Controls.Add(_treeView);
        _topSplit.Panel1.Controls.Add(sourceHeader);

        _destinationTreeView = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(25, 35, 25),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            ShowLines = true,
            ShowPlusMinus = true,
            HideSelection = false,
            Font = new Font("Segoe UI", 12f),
            AllowDrop = true
        };
        _destinationTreeView.BeforeExpand += DestinationTreeView_BeforeExpand;
        _destinationTreeView.DragEnter += DestinationTreeView_DragEnter;
        _destinationTreeView.DragOver += DestinationTreeView_DragOver;
        _destinationTreeView.DragDrop += DestinationTreeView_DragDrop;

        var destContextMenu = new ContextMenuStrip();
        destContextMenu.Items.Add("Refresh", null, (s, e) => RefreshAll());
        _destinationTreeView.ContextMenuStrip = destContextMenu;

        var destHeader = new Label
        {
            Text = "Move Target  ↓ drop files here",
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(120, 200, 120),
            BackColor = Color.FromArgb(35, 48, 35),
            Font = new Font("Segoe UI", 10f),
            Padding = new Padding(4, 2, 0, 0)
        };
        _topSplit.Panel2.Controls.Add(_destinationTreeView);
        _topSplit.Panel2.Controls.Add(destHeader);

        _splitContainer.Panel1.Controls.Add(_topSplit);
        _splitContainer.Panel2.Controls.Add(_fileListView);

        Controls.Add(_splitContainer);
        Controls.Add(pathPanel);

        LoadDrives();
        LoadDestinationDrives();
    }

    public event Action? RenameRequested;
    public event Action? MoveRequested;
    public event Action? DeleteRequested;
    public event Action<string[], string>? FilesDroppedOnDestination;

    public string[] GetSelectedFiles()
    {
        var files = new List<string>();
        foreach (ListViewItem item in _fileListView.SelectedItems)
        {
            if (item.Tag is string path)
                files.Add(path);
        }
        return files.ToArray();
    }

    public string[] GetAllFiles()
    {
        var files = new List<string>();
        foreach (ListViewItem item in _fileListView.Items)
        {
            if (item.Tag is string path)
                files.Add(path);
        }
        return files.ToArray();
    }

    public void NavigateToDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        _currentDirectory = path;
        _pathTextBox.Text = path;
        LoadFiles(path);
        DirectoryChanged?.Invoke(path);
    }

    public void RefreshFileList()
    {
        LoadFiles(_currentDirectory);
    }

    public void RefreshAll()
    {
        string? destSelected = _destinationTreeView.SelectedNode?.Tag as string;

        LoadDrives();
        ExpandToDirectory(_currentDirectory);

        LoadDestinationDrives();
        string destPath = destSelected != null && Directory.Exists(destSelected)
            ? destSelected
            : @"C:\Users\evan\OneDrive\Pictures\Display\Fisher\PapersCardsObjects";
        ExpandDestinationToDirectory(destPath);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _topSplit.SplitterDistance = Math.Max(50, _splitContainer.Panel1.Width / 2);
        ExpandToDirectory(_currentDirectory);
        ExpandDestinationToDirectory(@"C:\Users\evan\OneDrive\Pictures\Display\Fisher\PapersCardsObjects");
    }

    private void ExpandToDirectory(string targetPath)
    {
        if (!Directory.Exists(targetPath)) return;

        targetPath = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar);
        string root = Path.GetPathRoot(targetPath) ?? "";
        string rootKey = root.TrimEnd(Path.DirectorySeparatorChar);

        TreeNode? currentNode = null;
        foreach (TreeNode node in _treeView.Nodes)
        {
            if (node.Tag is string tag &&
                tag.TrimEnd(Path.DirectorySeparatorChar).Equals(rootKey, StringComparison.OrdinalIgnoreCase))
            {
                currentNode = node;
                break;
            }
        }

        if (currentNode == null) return;

        string[] parts = targetPath[root.Length..]
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        string currentPath = root;
        foreach (string part in parts)
        {
            currentNode.Expand(); // triggers BeforeExpand to load real children
            currentPath = Path.Combine(currentPath, part);

            TreeNode? next = null;
            foreach (TreeNode child in currentNode.Nodes)
            {
                if (child.Tag is string childTag &&
                    Path.GetFullPath(childTag).TrimEnd(Path.DirectorySeparatorChar)
                        .Equals(currentPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    next = child;
                    break;
                }
            }

            if (next == null) break;
            currentNode = next;
        }

        _suppressNavigate = true;
        _treeView.SelectedNode = currentNode;
        _suppressNavigate = false;
        currentNode.EnsureVisible();

        NavigateToDirectory(targetPath);
    }

    private void ExpandDestinationToDirectory(string targetPath)
    {
        if (!Directory.Exists(targetPath)) return;

        targetPath = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar);
        string root = Path.GetPathRoot(targetPath) ?? "";
        string rootKey = root.TrimEnd(Path.DirectorySeparatorChar);

        TreeNode? currentNode = null;
        foreach (TreeNode node in _destinationTreeView.Nodes)
        {
            if (node.Tag is string tag &&
                tag.TrimEnd(Path.DirectorySeparatorChar).Equals(rootKey, StringComparison.OrdinalIgnoreCase))
            {
                currentNode = node;
                break;
            }
        }

        if (currentNode == null) return;

        string[] parts = targetPath[root.Length..]
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        string currentPath = root;
        foreach (string part in parts)
        {
            currentNode.Expand();
            currentPath = Path.Combine(currentPath, part);

            TreeNode? next = null;
            foreach (TreeNode child in currentNode.Nodes)
            {
                if (child.Tag is string childTag &&
                    Path.GetFullPath(childTag).TrimEnd(Path.DirectorySeparatorChar)
                        .Equals(currentPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    next = child;
                    break;
                }
            }

            if (next == null) break;
            currentNode = next;
        }

        _destinationTreeView.SelectedNode = currentNode;
        currentNode.EnsureVisible();
    }

    private void LoadDrives()
    {
        _treeView.Nodes.Clear();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            var node = new TreeNode(drive.Name.TrimEnd('\\'))
            {
                Tag = drive.Name
            };
            node.Nodes.Add("__placeholder__");
            _treeView.Nodes.Add(node);
        }

        // Also add common folders
        AddSpecialFolder("Pictures", Environment.SpecialFolder.MyPictures);
        AddSpecialFolder("Documents", Environment.SpecialFolder.MyDocuments);
        AddSpecialFolder("Desktop", Environment.SpecialFolder.Desktop);
    }

    private void AddSpecialFolder(string name, Environment.SpecialFolder folder)
    {
        string path = Environment.GetFolderPath(folder);
        if (Directory.Exists(path))
        {
            var node = new TreeNode(name) { Tag = path };
            node.Nodes.Add("__placeholder__");
            _treeView.Nodes.Add(node);
        }
    }

    private void LoadDestinationDrives()
    {
        _destinationTreeView.Nodes.Clear();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            var node = new TreeNode(drive.Name.TrimEnd('\\')) { Tag = drive.Name };
            node.Nodes.Add("__placeholder__");
            _destinationTreeView.Nodes.Add(node);
        }
        AddDestinationSpecialFolder("Pictures", Environment.SpecialFolder.MyPictures);
        AddDestinationSpecialFolder("Documents", Environment.SpecialFolder.MyDocuments);
        AddDestinationSpecialFolder("Desktop", Environment.SpecialFolder.Desktop);
    }

    private void AddDestinationSpecialFolder(string name, Environment.SpecialFolder folder)
    {
        string path = Environment.GetFolderPath(folder);
        if (Directory.Exists(path))
        {
            var node = new TreeNode(name) { Tag = path };
            node.Nodes.Add("__placeholder__");
            _destinationTreeView.Nodes.Add(node);
        }
    }

    private void DestinationTreeView_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node == null) return;
        if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "__placeholder__")
        {
            e.Node.Nodes.Clear();
            if (e.Node.Tag is not string path) return;
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if ((dirInfo.Attributes & FileAttributes.Hidden) != 0) continue;
                    var node = new TreeNode(dirInfo.Name) { Tag = dir };
                    try { if (Directory.GetDirectories(dir).Length > 0) node.Nodes.Add("__placeholder__"); } catch { }
                    e.Node.Nodes.Add(node);
                }
            }
            catch { }
        }
    }

    private void DestinationTreeView_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Move
            : DragDropEffects.None;
    }

    private void DestinationTreeView_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
        {
            e.Effect = DragDropEffects.None;
            return;
        }
        var pt = _destinationTreeView.PointToClient(new Point(e.X, e.Y));
        var node = _destinationTreeView.GetNodeAt(pt);
        if (node?.Tag is string path && Directory.Exists(path))
        {
            _destinationTreeView.SelectedNode = node;
            e.Effect = DragDropEffects.Move;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void DestinationTreeView_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true) return;
        var pt = _destinationTreeView.PointToClient(new Point(e.X, e.Y));
        var node = _destinationTreeView.GetNodeAt(pt);
        if (node?.Tag is not string destPath || !Directory.Exists(destPath)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
        FilesDroppedOnDestination?.Invoke(files, destPath);
    }

    private void TreeView_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node == null) return;

        if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "__placeholder__")
        {
            e.Node.Nodes.Clear();
            string? path = e.Node.Tag as string;
            if (path == null) return;

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if ((dirInfo.Attributes & FileAttributes.Hidden) != 0) continue;

                    var node = new TreeNode(dirInfo.Name) { Tag = dir };
                    try
                    {
                        if (Directory.GetDirectories(dir).Length > 0)
                            node.Nodes.Add("__placeholder__");
                    }
                    catch { }
                    e.Node.Nodes.Add(node);
                }
            }
            catch { }
        }
    }

    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (_suppressNavigate) return;
        if (e.Node?.Tag is string path && Directory.Exists(path))
            NavigateToDirectory(path);
    }

    private void LoadFiles(string directory)
    {
        _fileListView.Items.Clear();
        _thumbnailList.Images.Clear();

        try
        {
            var files = Directory.GetFiles(directory)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            var items = new List<(string Path, FileInfo Info)>();
            foreach (var file in files)
            {
                items.Add((file, new FileInfo(file)));
            }

            // Sort
            items = _sortMode switch
            {
                SortMode.Name => _sortAscending
                    ? items.OrderBy(x => x.Info.Name).ToList()
                    : items.OrderByDescending(x => x.Info.Name).ToList(),
                SortMode.DateModified => _sortAscending
                    ? items.OrderBy(x => x.Info.LastWriteTime).ToList()
                    : items.OrderByDescending(x => x.Info.LastWriteTime).ToList(),
                SortMode.FileSize => _sortAscending
                    ? items.OrderBy(x => x.Info.Length).ToList()
                    : items.OrderByDescending(x => x.Info.Length).ToList(),
                _ => items.OrderBy(x => x.Info.Name).ToList()
            };

            foreach (var (filePath, info) in items)
            {
                string sizeStr = info.Length < 1024 * 1024
                    ? $"{info.Length / 1024.0:F0} KB"
                    : $"{info.Length / (1024.0 * 1024.0):F1} MB";

                int imageIndex = -1;
                try
                {
                    using var img = Image.FromFile(filePath);
                    using var thumb = img.GetThumbnailImage(160, 120, null, IntPtr.Zero);
                    _thumbnailList.Images.Add(thumb);
                    imageIndex = _thumbnailList.Images.Count - 1;
                }
                catch { }

                var lvi = new ListViewItem(info.Name, imageIndex)
                {
                    Tag = filePath
                };
                lvi.SubItems.Add(info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                lvi.SubItems.Add(sizeStr);
                lvi.SubItems.Add("");
                _fileListView.Items.Add(lvi);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private void FileListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var files = GetSelectedFiles();
        if (files.Length == 1)
            FileSelected?.Invoke(files[0]);
        if (files.Length > 0)
            FilesSelected?.Invoke(files);
    }

    private void FileListView_ColumnClick(object? sender, ColumnClickEventArgs e)
    {
        var newSort = e.Column switch
        {
            0 => SortMode.Name,
            1 => SortMode.DateModified,
            2 => SortMode.FileSize,
            _ => SortMode.Name
        };

        if (newSort == _sortMode)
            _sortAscending = !_sortAscending;
        else
        {
            _sortMode = newSort;
            _sortAscending = true;
        }

        LoadFiles(_currentDirectory);
    }

    private void FileListView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        var files = GetSelectedFiles();
        if (files.Length > 0)
        {
            DoDragDrop(new DataObject(DataFormats.FileDrop, files), DragDropEffects.Move);
        }
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var fbd = new FolderBrowserDialog
        {
            SelectedPath = _currentDirectory,
            Description = "Select a folder to browse"
        };
        if (fbd.ShowDialog() == DialogResult.OK)
        {
            NavigateToDirectory(fbd.SelectedPath);
        }
    }

    private void RenameSelected() => RenameRequested?.Invoke();
    private void MoveSelected() => MoveRequested?.Invoke();
    private void DeleteSelected() => DeleteRequested?.Invoke();

    private void CopyPathSelected()
    {
        var files = GetSelectedFiles();
        if (files.Length > 0)
            Clipboard.SetText(string.Join(Environment.NewLine, files));
    }

    private void OpenInExplorer()
    {
        System.Diagnostics.Process.Start("explorer.exe", _currentDirectory);
    }

    public void SelectFileByPath(string filePath)
    {
        foreach (ListViewItem item in _fileListView.Items)
        {
            if (item.Tag is string path && path == filePath)
            {
                item.Selected = true;
                item.EnsureVisible();
                break;
            }
        }
    }
}
