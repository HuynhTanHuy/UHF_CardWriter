using CareHR.UhfCardWriter.App.Presentation;

namespace CareHR.UhfCardWriter.App.Controls;

/// <summary>
/// Compact batch statistics — 2 rows × 3 columns. Operator-friendly, not a dashboard.
/// </summary>
public sealed class BatchResultPanel : UserControl
{
    public const int PreferredPanelHeight = 128;

    private readonly Label _written;
    private readonly Label _success;
    private readonly Label _failed;
    private readonly Label _remaining;
    private readonly Label _elapsed;
    private readonly Label _current;

    public BatchResultPanel()
    {
        SuspendLayout();
        Dock = DockStyle.None;
        BackColor = UiColors.White;
        Padding = new Padding(10, 8, 10, 8);
        BorderStyle = BorderStyle.FixedSingle;
        Size = new Size(400, PreferredPanelHeight);
        MinimumSize = new Size(0, PreferredPanelHeight);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0),
        };
        for (var c = 0; c < 3; c++)
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        _written = AddMetric(root, 0, 0, UiGlyphs.Written, "Written", "0", UiColors.CareHrBlue);
        _success = AddMetric(root, 1, 0, UiGlyphs.Success, "Success", "0", UiColors.Success);
        _failed = AddMetric(root, 2, 0, UiGlyphs.Failed, "Failed", "0", UiColors.Error);
        _remaining = AddMetric(root, 0, 1, UiGlyphs.Remaining, "Remaining", "0", UiColors.CareHrBlue);
        _elapsed = AddMetric(root, 1, 1, UiGlyphs.Elapsed, "Elapsed", "00:00:00", UiColors.TextPrimary);
        _current = AddMetric(root, 2, 1, UiGlyphs.Current, "Current", "1", UiColors.TextPrimary);

        Controls.Add(root);
        ResumeLayout(false);
    }

    public void ResetSession(int current, int remaining, int start, int end)
    {
        Update(
            written: 0,
            current: current,
            remaining: remaining,
            success: 0,
            failed: 0,
            elapsed: TimeSpan.Zero,
            completed: 0,
            total: Math.Max(0, end - start + 1),
            start: start,
            end: end);
    }

    public void Update(
        int written,
        int current,
        int remaining,
        int success,
        int failed,
        TimeSpan elapsed,
        int completed,
        int total,
        int start,
        int end)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Update(written, current, remaining, success, failed, elapsed, completed, total, start, end));
            return;
        }

        _written.Text = written.ToString();
        _success.Text = success.ToString();
        _failed.Text = failed.ToString();
        _remaining.Text = remaining.ToString();
        _elapsed.Text = elapsed.ToString(@"hh\:mm\:ss");
        _current.Text = current.ToString();
        _ = completed;
        _ = total;
        _ = start;
        _ = end;
    }

    private static Label AddMetric(
        TableLayoutPanel grid,
        int col,
        int row,
        string glyph,
        string caption,
        string value,
        Color valueColor)
    {
        var cell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(4, 2, 4, 2),
        };
        cell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cell.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        cell.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var icon = UiGlyphs.CreateIconLabel(glyph, 9f, UiColors.IconIdle);
        icon.Dock = DockStyle.Fill;

        var captionLabel = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8f),
            ForeColor = UiColors.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var valueLabel = new Label
        {
            Text = value,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
            ForeColor = valueColor,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            Margin = new Padding(22, 0, 0, 0),
        };

        cell.Controls.Add(icon, 0, 0);
        cell.Controls.Add(captionLabel, 1, 0);
        cell.SetColumnSpan(valueLabel, 2);
        cell.Controls.Add(valueLabel, 0, 1);

        grid.Controls.Add(cell, col, row);
        return valueLabel;
    }
}
