using System.Runtime.Versioning;
using LetterViewer.Models;

namespace LetterViewer.Dialogs;

[SupportedOSPlatform("windows")]
public class CombineImagesDialog : Form
{
    private readonly RadioButton _rightRadio;
    private readonly RadioButton _belowRadio;
    private readonly NumericUpDown _spacingUpDown;
    private readonly CheckBox _normalizeSizeCheck;
    private readonly TextBox _outputNameBox;
    private readonly string[] _files;

    public CombineDirection Direction => _rightRadio.Checked ? CombineDirection.Right : CombineDirection.Below;
    public int Spacing => (int)_spacingUpDown.Value;
    public bool NormalizeSize => _normalizeSizeCheck.Checked;
    public string OutputFileName => _outputNameBox.Text;

    public CombineImagesDialog(string[] selectedFiles)
    {
        _files = selectedFiles;
        Text = "Combine Images";
        Size = new Size(420, 330);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(37, 37, 38);

        var filesLabel = new Label
        {
            Text = $"Combining {selectedFiles.Length} images:",
            Location = new Point(15, 15),
            ForeColor = Color.White,
            AutoSize = true
        };

        var fileList = new ListBox
        {
            Location = new Point(15, 35),
            Size = new Size(375, 60),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White
        };
        foreach (var f in selectedFiles)
            fileList.Items.Add(Path.GetFileName(f));

        var dirLabel = new Label
        {
            Text = "Direction:",
            Location = new Point(15, 105),
            ForeColor = Color.White,
            AutoSize = true
        };

        _rightRadio = new RadioButton
        {
            Text = "Append to the right (side by side)",
            Location = new Point(100, 103),
            ForeColor = Color.White,
            AutoSize = true,
            Checked = true
        };

        _belowRadio = new RadioButton
        {
            Text = "Append below (stacked)",
            Location = new Point(100, 125),
            ForeColor = Color.White,
            AutoSize = true
        };

        var spacingLabel = new Label
        {
            Text = "Gap (px):",
            Location = new Point(15, 155),
            ForeColor = Color.White,
            AutoSize = true
        };

        _spacingUpDown = new NumericUpDown
        {
            Location = new Point(100, 153),
            Size = new Size(80, 25),
            Value = 30,
            Minimum = 0,
            Maximum = 100
        };

        _normalizeSizeCheck = new CheckBox
        {
            Text = "Normalize image sizes before combining",
            Location = new Point(15, 185),
            ForeColor = Color.White,
            AutoSize = true,
            Checked = true
        };

        var outputLabel = new Label
        {
            Text = "Output file:",
            Location = new Point(15, 218),
            ForeColor = Color.White,
            AutoSize = true
        };

        string defaultName = Path.GetFileNameWithoutExtension(selectedFiles[0]) + " c" +
                             Path.GetExtension(selectedFiles[0]);
        _outputNameBox = new TextBox
        {
            Text = defaultName,
            Location = new Point(100, 216),
            Size = new Size(290, 25),
            BackColor = Color.FromArgb(51, 51, 51),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var okBtn = new Button
        {
            Text = "Combine",
            Location = new Point(220, 255),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(310, 255),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };

        Controls.AddRange(new Control[] {
            filesLabel, fileList, dirLabel, _rightRadio, _belowRadio,
            spacingLabel, _spacingUpDown, _normalizeSizeCheck, outputLabel, _outputNameBox,
            okBtn, cancelBtn
        });

        AcceptButton = okBtn;
        CancelButton = cancelBtn;
    }
}
