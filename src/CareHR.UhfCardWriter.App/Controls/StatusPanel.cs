using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Presentation;

namespace CareHR.UhfCardWriter.App.Controls;

/// <summary>
/// Operator status: illustration + status strip matching CareHR Card Writer layout.
/// Strip layout: [indicator] [title] [detail…] [HH:mm:ss]
/// Only the indicator animates while Running/Starting/Stopping.
/// </summary>
public sealed class StatusPanel : UserControl
{
    private static readonly string[] RunFrames = ["○", "◔", "◑", "◕", "●", "◕", "◑", "◔"];

    private readonly PictureBox _illustration;
    private readonly Label _indicator;
    private readonly Label _title;
    private readonly Label _detail;
    private readonly Label _time;
    private readonly Panel _statusStrip;
    private ThemeSettings _theme = new();
    private System.Windows.Forms.Timer? _animTimer;
    private int _animFrame;
    private bool _animating;

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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

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
            BackColor = StripTeal,
            Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(0),
        };

        var stripLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0),
        };
        stripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
        stripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        stripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        stripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));

        _indicator = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Symbol", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(120, 220, 150),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "✓",
            Margin = new Padding(0),
        };

        _title = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Ready",
            Margin = new Padding(2, 0, 0, 0),
        };

        _detail = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(220, 235, 235),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Connect a desk reader to begin.",
            AutoEllipsis = true,
            Margin = new Padding(8, 0, 8, 0),
        };

        _time = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight,
            Text = DateTime.Now.ToString("HH:mm:ss"),
            Margin = new Padding(0),
        };

        stripLayout.Controls.Add(_indicator, 0, 0);
        stripLayout.Controls.Add(_title, 1, 0);
        stripLayout.Controls.Add(_detail, 2, 0);
        stripLayout.Controls.Add(_time, 3, 0);
        _statusStrip.Controls.Add(stripLayout);

        root.Controls.Add(_illustration, 0, 0);
        root.Controls.Add(_statusStrip, 0, 1);
        Controls.Add(root);
        ResumeLayout(false);

        Disposed += (_, _) => StopAnimation();
    }

    public void ApplyTheme(ThemeSettings theme) => _theme = theme ?? new ThemeSettings();

    public void SetState(UiState state, string detail)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetState(state, detail));
            return;
        }

        var detailText = detail ?? string.Empty;
        var (title, stripColor, idleMark, indicatorColor, shouldAnimate) = Map(state, detailText);

        _statusStrip.BackColor = stripColor;
        _title.Text = title;
        _detail.Text = detailText;
        _time.Text = DateTime.Now.ToString("HH:mm:ss");
        _illustration.Image = StatusIllustrations.ForState(state);

        if (shouldAnimate)
        {
            _indicator.ForeColor = indicatorColor;
            StartAnimation();
        }
        else
        {
            StopAnimation();
            _indicator.Text = idleMark;
            _indicator.ForeColor = indicatorColor;
        }

        _ = _theme;
    }

    private void StartAnimation()
    {
        if (_animating && _animTimer is not null)
            return;

        _animating = true;
        _animFrame = 0;
        _indicator.Text = RunFrames[0];

        _animTimer ??= new System.Windows.Forms.Timer { Interval = 260 };
        _animTimer.Tick -= AnimTimer_Tick;
        _animTimer.Tick += AnimTimer_Tick;
        _animTimer.Start();
    }

    private void StopAnimation()
    {
        _animating = false;
        if (_animTimer is null)
            return;

        _animTimer.Stop();
        _animTimer.Tick -= AnimTimer_Tick;
        _animTimer.Dispose();
        _animTimer = null;
        _animFrame = 0;
    }

    private void AnimTimer_Tick(object? sender, EventArgs e)
    {
        if (!_animating)
        {
            StopAnimation();
            return;
        }

        _animFrame = (_animFrame + 1) % RunFrames.Length;
        _indicator.Text = RunFrames[_animFrame];
        // Soft green pulse on indicator only.
        var bright = _animFrame is 0 or 4;
        _indicator.ForeColor = bright
            ? Color.FromArgb(160, 240, 180)
            : Color.FromArgb(90, 200, 130);
        _time.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    private static (string Title, Color Strip, string IdleMark, Color Indicator, bool Animate) Map(
        UiState state,
        string detail)
    {
        var stopping = detail.Contains("STOPPING", StringComparison.OrdinalIgnoreCase)
                       || detail.Contains("Stopping", StringComparison.OrdinalIgnoreCase);
        var starting = detail.Contains("Starting", StringComparison.OrdinalIgnoreCase)
                       || detail.Contains("Connecting", StringComparison.OrdinalIgnoreCase);
        var stopped = detail.Contains("STOPPED", StringComparison.OrdinalIgnoreCase)
                      || detail.StartsWith("Stopped", StringComparison.OrdinalIgnoreCase);

        var green = Color.FromArgb(120, 220, 150);
        var white = Color.FromArgb(235, 245, 245);
        var amber = Color.FromArgb(255, 200, 120);
        var red = Color.FromArgb(255, 180, 180);

        return state switch
        {
            UiState.Disconnected => ("Disconnected", StripNeutral, "○", white, false),
            UiState.Ready when stopped => ("Stopped", StripTeal, "✓", green, false),
            UiState.Ready => ("Ready", StripTeal, "✓", green, false),
            UiState.Busy when stopping => ("Stopping", StripAmber, "◌", amber, true),
            UiState.Busy when starting => ("Starting", StripAmber, "◐", amber, true),
            UiState.Busy => ("Working", StripAmber, "◐", amber, true),
            UiState.WaitingForCard => ("Running", StripTeal, "◉", green, true),
            UiState.Scanning => ("Running", StripTeal, "◉", green, true),
            UiState.Writing => ("Writing", StripAmber, "◉", amber, true),
            UiState.Verifying => ("Verifying", StripTeal, "◉", green, true),
            UiState.Registering => ("Registering", StripTeal, "◉", green, true),
            UiState.Success => ("Completed", StripGreen, "✓", green, false),
            UiState.Done => ("Completed", StripGreen, "✓", green, false),
            UiState.Failed => ("Error", StripRed, "!", red, false),
            _ => ("Ready", StripTeal, "✓", green, false),
        };
    }

    // Match screenshot: CareHR teal strip (slightly deepened for readability).
    private static Color StripTeal => Color.FromArgb(18, 105, 110);
    private static Color StripGreen => Color.FromArgb(40, 110, 70);
    private static Color StripAmber => Color.FromArgb(140, 95, 40);
    private static Color StripRed => Color.FromArgb(140, 55, 55);
    private static Color StripNeutral => Color.FromArgb(70, 80, 86);
}
