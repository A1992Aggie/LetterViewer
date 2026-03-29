using System.Runtime.Versioning;

namespace LetterViewer.Dialogs;

[SupportedOSPlatform("windows")]
public class MoveFileDialog : Form
{
    private readonly TextBox _destBox;
    private readonly ListBox _recentList;
    private static readonly List<string> _recentDirectories = new();

    public string DestinationDirectory => _destBox.Text;

    public MoveFileDialog(string[] filesToMove, string currentDirectory)
    {
        Text = $"Move {filesToMove.Length} file(s)";
        Size = new Size(500, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(37, 37, 38);

        var filesLabel = new Label
        {
            Text = $"Moving: {string.Join(", ", filesToMove.Select(Path.GetFileName).Take(5))}" +
                   (filesToMove.Length > 5 ? $" (+{filesToMove.Length - 5} more)" : ""),
            Location = new Point(15, 15),
            Size = new Size(460, 20),
            ForeColor = Color.White,
            AutoEllipsis = true
        };

        var destLabel = new Label
        {
            Text = "Destination:",
            Location = new Point(15, 45),
            ForeColor = Color.White,
            AutoSize = true
        };

        _destBox = new TextBox
        {
            Location = new Point(15, 65),
            Size = new Size(400, 25),
            Text = currentDirectory,
            BackColor = Color.FromArgb(51, 51, 51),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var browseBtn = new Button
        {
            Text = "...",
            Location = new Point(420, 64),
            Size = new Size(40, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        browseBtn.Click += (s, e) =>
        {
            using var fbd = new FolderBrowserDialog { SelectedPath = _destBox.Text };
            if (fbd.ShowDialog() == DialogResult.OK)
                _destBox.Text = fbd.SelectedPath;
        };

        var recentLabel = new Label
        {
            Text = "Recent destinations:",
            Location = new Point(15, 100),
            ForeColor = Color.White,
            AutoSize = true
        };

        _recentList = new ListBox
        {
            Location = new Point(15, 120),
            Size = new Size(450, 130),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White
        };
        foreach (var dir in _recentDirectories)
            _recentList.Items.Add(dir);
        _recentList.DoubleClick += (s, e) =>
        {
            if (_recentList.SelectedItem is string dir)
                _destBox.Text = dir;
        };

        var okBtn = new Button
        {
            Text = "Move",
            Location = new Point(310, 270),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(400, 270),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };

        Controls.AddRange(new Control[] {
            filesLabel, destLabel, _destBox, browseBtn,
            recentLabel, _recentList, okBtn, cancelBtn
        });

        AcceptButton = okBtn;
        CancelButton = cancelBtn;
    }

    public static void AddRecentDirectory(string dir)
    {
        _recentDirectories.Remove(dir);
        _recentDirectories.Insert(0, dir);
        if (_recentDirectories.Count > 10)
            _recentDirectories.RemoveAt(10);
    }
}
