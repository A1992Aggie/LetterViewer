using System.Runtime.Versioning;

namespace LetterViewer.Dialogs;

[SupportedOSPlatform("windows")]
public class OcrResultDialog : Form
{
    private readonly TextBox _resultBox;

    public string OcrText => _resultBox.Text;
    public bool CombineRequested { get; private set; }

    public OcrResultDialog(string text, string fileName)
    {
        Text = $"OCR Result - {fileName}";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(37, 37, 38);

        _resultBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = false,
            Text = text,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Font = new Font("Consolas", 10f),
            WordWrap = true
        };

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = Color.FromArgb(37, 37, 38)
        };

        var copyBtn = new Button
        {
            Text = "Copy to Clipboard",
            Location = new Point(10, 5),
            Size = new Size(130, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White
        };
        copyBtn.Click += (s, e) =>
        {
            if (!string.IsNullOrEmpty(_resultBox.Text))
            {
                Clipboard.SetText(_resultBox.Text);
                copyBtn.Text = "Copied!";
                var timer = new System.Windows.Forms.Timer { Interval = 1500 };
                timer.Tick += (s2, e2) => { copyBtn.Text = "Copy to Clipboard"; timer.Stop(); timer.Dispose(); };
                timer.Start();
            }
        };

        var combineBtn = new Button
        {
            Text = "Combine with Image",
            Location = new Point(150, 5),
            Size = new Size(145, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 100, 60),
            ForeColor = Color.White
        };
        combineBtn.Click += (s, e) =>
        {
            CombineRequested = true;
            DialogResult = DialogResult.OK;
            Close();
        };

        var closeBtn = new Button
        {
            Text = "Close",
            Location = new Point(490, 5),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };

        buttonPanel.Controls.Add(copyBtn);
        buttonPanel.Controls.Add(combineBtn);
        buttonPanel.Controls.Add(closeBtn);

        Controls.Add(_resultBox);
        Controls.Add(buttonPanel);

        AcceptButton = closeBtn;
    }
}

