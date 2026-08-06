using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Presentation;

namespace CareHR.UhfCardWriter.App.Controls;

/// <summary>Large status banner for the operator (READY / WRITING / SUCCESS / …).</summary>
public sealed class StatusPanel : UserControl
{
    private readonly Label _title;
    private readonly Label _detail;
    private ThemeSettings _theme = new();

    public StatusPanel()
    {
        SuspendLayout();
        Dock = DockStyle.Fill;
        Padding = new Padding(16);
        BackColor = Color.FromArgb(69, 90, 100);

        _title = new Label
        {
            Dock = DockStyle.Top,
            Height = 48,
            Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "READY",
        };

        _detail = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(230, 230, 230),
            TextAlign = ContentAlignment.TopLeft,
            Text = "Connect a reader to begin.",
        };

        Controls.Add(_detail);
        Controls.Add(_title);
        ResumeLayout(false);
    }

    public void ApplyTheme(ThemeSettings theme) => _theme = theme ?? new ThemeSettings();

    public void SetState(UiState state, string detail)
    {
        var (text, color) = state switch
        {
            UiState.Disconnected => ("READY", UiInputHelper.ParseColor(_theme.NeutralHex, Color.FromArgb(69, 90, 100))),
            UiState.Connected => ("CONNECTED", UiInputHelper.ParseColor(_theme.AccentHex, Color.FromArgb(13, 115, 119))),
            UiState.Scanning => ("SCANNING", UiInputHelper.ParseColor(_theme.AccentHex, Color.FromArgb(13, 115, 119))),
            UiState.Writing => ("WRITING", UiInputHelper.ParseColor(_theme.WarningHex, Color.FromArgb(239, 108, 0))),
            UiState.Verifying => ("VERIFYING", UiInputHelper.ParseColor(_theme.WarningHex, Color.FromArgb(239, 108, 0))),
            UiState.Registering => ("REGISTERING", UiInputHelper.ParseColor(_theme.WarningHex, Color.FromArgb(239, 108, 0))),
            UiState.Completed => ("SUCCESS", UiInputHelper.ParseColor(_theme.SuccessHex, Color.FromArgb(46, 125, 50))),
            UiState.Error => ("ERROR", UiInputHelper.ParseColor(_theme.ErrorHex, Color.FromArgb(198, 40, 40))),
            UiState.Busy => ("BUSY", UiInputHelper.ParseColor(_theme.WarningHex, Color.FromArgb(239, 108, 0))),
            _ => ("READY", UiInputHelper.ParseColor(_theme.AccentHex, Color.FromArgb(13, 115, 119))),
        };

        BackColor = color;
        _title.Text = text;
        _detail.Text = detail ?? string.Empty;
    }
}
