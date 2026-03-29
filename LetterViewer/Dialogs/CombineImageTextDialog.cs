using System.Runtime.Versioning;

namespace LetterViewer.Dialogs;

[SupportedOSPlatform("windows")]
public class CombineImageTextDialog : Form
{
    private readonly TextBox _textBox;
    private readonly TextBox _outputNameBox;

    public string PastedText => _textBox.Text;
    public string OutputFileName => _outputNameBox.Text;

    public CombineImageTextDialog(string imagePath)
    {
        Text = "Combine Image with Text";
        Size = new Size(640, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(400, 350);
        MaximizeBox = true;
        MinimizeBox = false;
        BackColor = Color.FromArgb(37, 37, 38);

        // Image info strip at top
        var imageLabel = new Label
        {
            Text = $"Image:  {Path.GetFileName(imagePath)}",
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(8, 6, 0, 0),
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.FromArgb(30, 30, 30)
        };

        // Paste label
        var pasteLabel = new Label
        {
            Text = "Paste text:",
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(8, 4, 0, 0),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f)
        };

        // Large text input
        _textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            AcceptsTab = false,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Font = new Font("Consolas", 10f),
            BorderStyle = BorderStyle.None
        };

        // Bottom strip: output name + buttons
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = Color.FromArgb(30, 30, 30),
            Padding = new Padding(8, 8, 8, 8)
        };

        var outputLabel = new Label
        {
            Text = "Output:",
            Location = new Point(8, 14),
            ForeColor = Color.White,
            AutoSize = true
        };

        string baseName = Path.GetFileNameWithoutExtension(imagePath);
        _outputNameBox = new TextBox
        {
            Text = $"ocr_{baseName}.jpg",
            Location = new Point(62, 11),
            Size = new Size(320, 25),
            BackColor = Color.FromArgb(51, 51, 51),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var combineBtn = new Button
        {
            Text = "Combine",
            Location = new Point(392, 10),
            Size = new Size(90, 27),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            DialogResult = DialogResult.OK
        };
        combineBtn.Click += CombineBtn_Click;

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(490, 10),
            Size = new Size(80, 27),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            DialogResult = DialogResult.Cancel
        };

        bottomPanel.Controls.AddRange(new Control[] { outputLabel, _outputNameBox, combineBtn, cancelBtn });

        // Add in reverse dock order (Fill must be added last)
        Controls.Add(_textBox);
        Controls.Add(pasteLabel);
        Controls.Add(imageLabel);
        Controls.Add(bottomPanel);

        AcceptButton = combineBtn;
        CancelButton = cancelBtn;

        ActiveControl = _textBox;
    }

    private void CombineBtn_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_textBox.Text))
        {
            MessageBox.Show("Please paste some text before combining.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        if (string.IsNullOrWhiteSpace(_outputNameBox.Text))
        {
            MessageBox.Show("Please enter an output file name.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
    }
}
