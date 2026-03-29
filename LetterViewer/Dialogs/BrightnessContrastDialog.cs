using System.Runtime.Versioning;

namespace LetterViewer.Dialogs;

[SupportedOSPlatform("windows")]
public class BrightnessContrastDialog : Form
{
    private readonly TrackBar _brightnessTrack;
    private readonly TrackBar _contrastTrack;
    private readonly Label _brightnessLabel;
    private readonly Label _contrastLabel;
    private readonly PictureBox _preview;
    private readonly Image _originalImage;

    public float Brightness => _brightnessTrack.Value / 100f + 1f;
    public float Contrast => _contrastTrack.Value / 100f + 1f;

    public BrightnessContrastDialog(Image originalImage)
    {
        _originalImage = originalImage;
        Text = "Brightness / Contrast";
        Size = new Size(500, 450);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(37, 37, 38);

        _preview = new PictureBox
        {
            Location = new Point(10, 10),
            Size = new Size(465, 260),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30),
            Image = originalImage
        };

        var brightnessLbl = new Label
        {
            Text = "Brightness:",
            Location = new Point(10, 285),
            ForeColor = Color.White,
            AutoSize = true
        };

        _brightnessTrack = new TrackBar
        {
            Location = new Point(100, 280),
            Size = new Size(300, 30),
            Minimum = -100,
            Maximum = 100,
            Value = 0,
            TickFrequency = 10
        };
        _brightnessTrack.ValueChanged += OnValueChanged;

        _brightnessLabel = new Label
        {
            Text = "0",
            Location = new Point(410, 285),
            ForeColor = Color.White,
            AutoSize = true
        };

        var contrastLbl = new Label
        {
            Text = "Contrast:",
            Location = new Point(10, 325),
            ForeColor = Color.White,
            AutoSize = true
        };

        _contrastTrack = new TrackBar
        {
            Location = new Point(100, 320),
            Size = new Size(300, 30),
            Minimum = -100,
            Maximum = 100,
            Value = 0,
            TickFrequency = 10
        };
        _contrastTrack.ValueChanged += OnValueChanged;

        _contrastLabel = new Label
        {
            Text = "0",
            Location = new Point(410, 325),
            ForeColor = Color.White,
            AutoSize = true
        };

        var okBtn = new Button
        {
            Text = "Apply",
            Location = new Point(300, 370),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(390, 370),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };

        var resetBtn = new Button
        {
            Text = "Reset",
            Location = new Point(10, 370),
            Size = new Size(80, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        resetBtn.Click += (s, e) =>
        {
            _brightnessTrack.Value = 0;
            _contrastTrack.Value = 0;
        };

        Controls.AddRange(new Control[] {
            _preview, brightnessLbl, _brightnessTrack, _brightnessLabel,
            contrastLbl, _contrastTrack, _contrastLabel,
            okBtn, cancelBtn, resetBtn
        });

        AcceptButton = okBtn;
        CancelButton = cancelBtn;
    }

    private void OnValueChanged(object? sender, EventArgs e)
    {
        _brightnessLabel.Text = _brightnessTrack.Value.ToString();
        _contrastLabel.Text = _contrastTrack.Value.ToString();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        try
        {
            using var original = new Bitmap(_originalImage);
            var adjusted = Services.ImageProcessingService.AdjustBrightnessContrast(
                original, Brightness, Contrast);
            _preview.Image?.Dispose();
            _preview.Image = adjusted;
        }
        catch { }
    }
}
