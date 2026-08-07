namespace CareHR.UhfCardWriter.App.Controls;

/// <summary>Business operation log (timestamp / action / result / duration). No SDK details.</summary>
public sealed class OperationLogControl : UserControl
{
    private const int MaxItems = 500;
    private readonly ListView _list;

    public OperationLogControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Font = new Font("Consolas", 9f),
        };
        _list.Columns.Add("Time", 80);
        _list.Columns.Add("Action", 110);
        _list.Columns.Add("Result", 300);
        _list.Columns.Add("ms", 60);

        Controls.Add(_list);
    }

    public void Append(string action, string result, long? durationMs = null)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Append(action, result, durationMs));
            return;
        }

        var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
        item.SubItems.Add(action);
        item.SubItems.Add(result);
        item.SubItems.Add(durationMs is null ? string.Empty : durationMs.Value.ToString());
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
        {
            var ms = item.SubItems.Count > 3 ? item.SubItems[3].Text : string.Empty;
            lines.Add($"{item.Text}\t{item.SubItems[1].Text}\t{item.SubItems[2].Text}\t{ms}");
        }

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
}
