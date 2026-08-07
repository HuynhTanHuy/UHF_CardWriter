using CareHR.UhfCardWriter.App.Presentation;

namespace CareHR.UhfCardWriter.App.Controls;

/// <summary>Compact operator log — plain language only.</summary>
public sealed class OperationLogControl : UserControl
{
    private const int MaxItems = 500;
    private readonly ListView _list;

    public OperationLogControl()
    {
        Dock = DockStyle.Fill;
        BackColor = UiColors.White;
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(0);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = UiColors.FieldFill,
            Padding = new Padding(8, 0, 8, 0),
        };
        var icon = UiGlyphs.CreateIconLabel(UiGlyphs.Log, 9f, UiColors.IconIdle);
        icon.Width = 20;
        icon.Dock = DockStyle.Left;
        var title = new Label
        {
            Text = "Log",
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9f),
            ForeColor = UiColors.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 6, 0, 0),
        };
        header.Controls.Add(title);
        header.Controls.Add(icon);

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Font = new Font("Segoe UI", 9f),
            BorderStyle = BorderStyle.None,
            OwnerDraw = false,
        };
        _list.Columns.Add("Time", 72);
        _list.Columns.Add("Event", 520);

        Controls.Add(_list);
        Controls.Add(header);
    }

    public void Append(string action, string result, long? durationMs = null)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Append(action, result, durationMs));
            return;
        }

        _ = durationMs;
        var message = FormatOperatorLine(action, result);
        var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
        item.SubItems.Add(message);
        _list.Items.Add(item);
        while (_list.Items.Count > MaxItems)
            _list.Items.RemoveAt(0);
        item.EnsureVisible();
    }

    public IEnumerable<string> GetLines()
    {
        if (InvokeRequired)
            return (IEnumerable<string>)Invoke(GetLines);

        var lines = new List<string>(_list.Items.Count);
        foreach (ListViewItem item in _list.Items)
            lines.Add($"{item.Text}\t{item.SubItems[1].Text}");

        return lines;
    }

    public void ClearLog()
    {
        if (InvokeRequired)
        {
            BeginInvoke(ClearLog);
            return;
        }

        _list.Items.Clear();
    }

    private static string FormatOperatorLine(string? action, string? result)
    {
        var a = (action ?? string.Empty).Trim();
        var r = (result ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(a))
            return r;
        if (string.IsNullOrEmpty(r))
            return a;
        // Prefer the human result when action is just a category tag.
        if (r.StartsWith(a, StringComparison.OrdinalIgnoreCase))
            return r;
        return r;
    }
}
