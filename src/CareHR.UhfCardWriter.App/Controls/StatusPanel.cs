using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Presentation;

namespace CareHR.UhfCardWriter.App.Controls;

/// <summary>
/// Compact operator status: illustration + colored status strip (CardWritter-inspired).
/// </summary>
public sealed class StatusPanel : UserControl
{
    private readonly PictureBox _illustration;
    private readonly Label _icon;
    private readonly Label _title;
    private readonly Label _detail;
    private readonly Panel _statusStrip;
    private ThemeSettings _theme = new();

    public StatusPanel()
    {
        SuspendLayout();
        Dock = DockStyle.Fill;
        BackColor = UiColors.White;
        Padding = new Padding(0);
        MinimumSize = new Size(160, 0);
        BorderStyle = BorderStyle.FixedSingle;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        _illustration = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White,
            Margin = new Padding(8, 8, 8, 0),
            Image = StatusIllustrations.ForState(UiState.Ready),
        };

        _statusStrip = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiColors.Neutral,
            Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(0),
        };

        var stripLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
        };
        stripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
        stripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        stripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        _icon = new Label
        {
            Dock = DockStyle.Fill,
            Font = UiGlyphs.IconFont(14f),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = UiGlyphs.Ready,
        };

        _title = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Ready",
        };

        _detail = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(230, 235, 238),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Connect a desk reader to begin.",
            AutoEllipsis = true,
        };

        stripLayout.Controls.Add(_icon, 0, 0);
        stripLayout.Controls.Add(_title, 1, 0);
        stripLayout.Controls.Add(_detail, 2, 0);
        _statusStrip.Controls.Add(stripLayout);

        root.Controls.Add(_illustration, 0, 0);
        root.Controls.Add(_statusStrip, 0, 1);
        Controls.Add(root);
        ResumeLayout(false);
    }

    public void ApplyTheme(ThemeSettings theme) => _theme = theme ?? new ThemeSettings();

    public void SetState(UiState state, string detail)
    {
        var (text, glyph, color) = Map(state);

        _statusStrip.BackColor = color;
        _icon.Text = glyph;
        _title.Text = text;
        _detail.Text = detail ?? string.Empty;
        _illustration.Image = StatusIllustrations.ForState(state);
    }

    private (string Text, string Glyph, Color Color) Map(UiState state) => state switch
    {
        UiState.Disconnected => ("Ready", UiGlyphs.Ready, Neutral()),
        UiState.Ready => ("Ready", UiGlyphs.Ready, Accent()),
        UiState.WaitingForCard => ("Place card", UiGlyphs.PlaceCard, Accent()),
        UiState.Scanning => ("Scanning…", UiGlyphs.Scanning, Accent()),
        UiState.Writing => ("Writing…", UiGlyphs.Writing, Warning()),
        UiState.Verifying => ("Verifying…", UiGlyphs.Verifying, Accent()),
        UiState.Registering => ("Registering…", UiGlyphs.Registering, Accent()),
        UiState.Success => ("Completed", UiGlyphs.Success, Success()),
        UiState.Done => ("Completed", UiGlyphs.Success, Success()),
        UiState.Failed => ("Error", UiGlyphs.Error, Error()),
        UiState.Busy => ("Working…", UiGlyphs.Scanning, Warning()),
        _ => ("Ready", UiGlyphs.Ready, Neutral()),
    };

    private Color Accent() => UiInputHelper.ParseColor(_theme.AccentHex, UiColors.CareHrBlue);
    private Color Success() => UiInputHelper.ParseColor(_theme.SuccessHex, UiColors.Success);
    private Color Error() => UiInputHelper.ParseColor(_theme.ErrorHex, UiColors.Error);
    private Color Warning() => UiInputHelper.ParseColor(_theme.WarningHex, UiColors.Warning);
    private Color Neutral() => UiInputHelper.ParseColor(_theme.NeutralHex, UiColors.Neutral);
}
