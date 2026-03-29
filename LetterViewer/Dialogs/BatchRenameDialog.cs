using System.Runtime.Versioning;
using LetterViewer.Models;

namespace LetterViewer.Dialogs;

[SupportedOSPlatform("windows")]
public class BatchRenameDialog : Form
{
    private readonly TextBox _prefixBox;
    private readonly TextBox _suffixBox;
    private readonly CheckBox _sequenceCheck;
    private readonly NumericUpDown _seqStart;
    private readonly NumericUpDown _seqPadding;
    private readonly TextBox _findBox;
    private readonly TextBox _replaceBox;
    private readonly CheckBox _regexCheck;
    private readonly CheckBox _datePrefixCheck;
    private readonly ListView _previewList;
    private readonly string[] _files;

    public BatchRenamePattern Pattern { get; private set; } = new();

    public BatchRenameDialog(string[] files)
    {
        _files = files;
        Text = "Batch Rename";
        Size = new Size(600, 550);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(37, 37, 38);

        int y = 15;

        AddLabel("Prefix:", 15, y);
        _prefixBox = AddTextBox(100, y, 200);
        y += 35;

        AddLabel("Suffix:", 15, y);
        _suffixBox = AddTextBox(100, y, 200);
        y += 35;

        AddLabel("Find:", 15, y);
        _findBox = AddTextBox(100, y, 200);
        _regexCheck = new CheckBox { Text = "Regex", Location = new Point(310, y), ForeColor = Color.White, AutoSize = true };
        Controls.Add(_regexCheck);
        y += 35;

        AddLabel("Replace:", 15, y);
        _replaceBox = AddTextBox(100, y, 200);
        y += 35;

        _sequenceCheck = new CheckBox { Text = "Add sequence number", Location = new Point(15, y), ForeColor = Color.White, AutoSize = true };
        Controls.Add(_sequenceCheck);
        y += 30;

        AddLabel("Start:", 30, y);
        _seqStart = new NumericUpDown { Location = new Point(100, y), Size = new Size(80, 25), Value = 1, Minimum = 0, Maximum = 9999 };
        Controls.Add(_seqStart);

        AddLabel("Padding:", 200, y);
        _seqPadding = new NumericUpDown { Location = new Point(270, y), Size = new Size(60, 25), Value = 3, Minimum = 1, Maximum = 6 };
        Controls.Add(_seqPadding);
        y += 35;

        _datePrefixCheck = new CheckBox { Text = "Add EXIF date prefix (YYYYMMDD_)", Location = new Point(15, y), ForeColor = Color.White, AutoSize = true };
        Controls.Add(_datePrefixCheck);
        y += 35;

        AddLabel("Preview:", 15, y);
        y += 20;

        _previewList = new ListView
        {
            Location = new Point(15, y),
            Size = new Size(555, 180),
            View = View.Details,
            FullRowSelect = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f)
        };
        _previewList.Columns.Add("Original", 260);
        _previewList.Columns.Add("New Name", 280);
        Controls.Add(_previewList);

        y += 190;

        var previewBtn = new Button
        {
            Text = "Preview",
            Location = new Point(380, y),
            Size = new Size(90, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        previewBtn.Click += (s, e) => UpdatePreview();
        Controls.Add(previewBtn);

        var okBtn = new Button
        {
            Text = "Rename",
            Location = new Point(380, y + 35),
            Size = new Size(90, 30),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White
        };
        okBtn.Click += (s, e) => BuildPattern();

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(480, y + 35),
            Size = new Size(90, 30),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };

        Controls.Add(okBtn);
        Controls.Add(cancelBtn);
        AcceptButton = okBtn;
        CancelButton = cancelBtn;

        // Wire up change events for live preview
        foreach (Control c in Controls)
        {
            if (c is TextBox tb) tb.TextChanged += (s, e) => UpdatePreview();
            if (c is CheckBox cb) cb.CheckedChanged += (s, e) => UpdatePreview();
            if (c is NumericUpDown nu) nu.ValueChanged += (s, e) => UpdatePreview();
        }

        UpdatePreview();
    }

    private Label AddLabel(string text, int x, int y)
    {
        var lbl = new Label { Text = text, Location = new Point(x, y + 3), ForeColor = Color.White, AutoSize = true };
        Controls.Add(lbl);
        return lbl;
    }

    private TextBox AddTextBox(int x, int y, int width)
    {
        var tb = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 25),
            BackColor = Color.FromArgb(51, 51, 51),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(tb);
        return tb;
    }

    private void BuildPattern()
    {
        Pattern = new BatchRenamePattern
        {
            Prefix = string.IsNullOrEmpty(_prefixBox.Text) ? null : _prefixBox.Text,
            Suffix = string.IsNullOrEmpty(_suffixBox.Text) ? null : _suffixBox.Text,
            AddSequenceNumber = _sequenceCheck.Checked,
            SequenceStart = (int)_seqStart.Value,
            SequencePadding = (int)_seqPadding.Value,
            FindText = string.IsNullOrEmpty(_findBox.Text) ? null : _findBox.Text,
            ReplaceText = _replaceBox.Text,
            UseRegex = _regexCheck.Checked,
            UseDatePrefix = _datePrefixCheck.Checked
        };
    }

    private void UpdatePreview()
    {
        BuildPattern();
        _previewList.Items.Clear();

        for (int i = 0; i < _files.Length; i++)
        {
            string originalName = Path.GetFileName(_files[i]);
            string newName = Pattern.Apply(originalName, i, null);
            var item = new ListViewItem(originalName);
            item.SubItems.Add(newName);
            if (originalName != newName)
                item.ForeColor = Color.LightGreen;
            _previewList.Items.Add(item);
        }
    }
}
