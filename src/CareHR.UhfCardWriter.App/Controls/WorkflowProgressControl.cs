using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Presentation;

namespace CareHR.UhfCardWriter.App.Controls;

/// <summary>Horizontal workflow step strip: Connect → Scan → Write → Verify → Register → Done.</summary>
public sealed class WorkflowProgressControl : UserControl
{
    private static readonly string[] Steps =
    {
        "Connect", "Scan", "Write", "Verify", "Register", "Done",
    };

    private readonly Label[] _labels;
    private int _activeIndex = -1;
    private Color _accent = Color.FromArgb(13, 115, 119);
    private Color _muted = Color.FromArgb(158, 158, 158);
    private Color _done = Color.FromArgb(46, 125, 50);

    public WorkflowProgressControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(8, 10, 8, 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = Steps.Length * 2 - 1,
            RowCount = 1,
        };

        _labels = new Label[Steps.Length];
        var col = 0;
        for (var i = 0; i < Steps.Length; i++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Steps.Length));
            _labels[i] = new Label
            {
                Dock = DockStyle.Fill,
                Text = Steps[i],
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = _muted,
            };
            layout.Controls.Add(_labels[i], col++, 0);

            if (i < Steps.Length - 1)
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
                layout.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "→",
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = _muted,
                    Font = new Font("Segoe UI", 9f),
                }, col++, 0);
            }
        }

        Controls.Add(layout);
    }

    public void ApplyTheme(ThemeSettings theme)
    {
        _accent = UiInputHelper.ParseColor(theme.AccentHex, _accent);
        _done = UiInputHelper.ParseColor(theme.SuccessHex, _done);
        RefreshStyles();
    }

    /// <summary>Sets active step index 0..5 (-1 = none).</summary>
    public void SetActiveStep(int index)
    {
        _activeIndex = index;
        RefreshStyles();
    }

    public void SetFromUiState(UiState state)
    {
        SetActiveStep(state switch
        {
            UiState.Disconnected => 0,
            UiState.Connected => 0,
            UiState.Scanning => 1,
            UiState.Writing => 2,
            UiState.Verifying => 3,
            UiState.Registering => 4,
            UiState.Completed => 5,
            UiState.Error => _activeIndex,
            UiState.Busy => _activeIndex,
            _ => -1,
        });
    }

    private void RefreshStyles()
    {
        for (var i = 0; i < _labels.Length; i++)
        {
            if (i < _activeIndex)
            {
                _labels[i].ForeColor = _done;
                _labels[i].Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            }
            else if (i == _activeIndex)
            {
                _labels[i].ForeColor = _accent;
                _labels[i].Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            }
            else
            {
                _labels[i].ForeColor = _muted;
                _labels[i].Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            }
        }
    }
}
