namespace CareHR.UhfCardWriter.App.Controls;

/// <summary>Business operation log (timestamp / action / result). No SDK details.</summary>
public sealed class OperationLogControl : UserControl
{
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
        _list.Columns.Add("Action", 120);
        _list.Columns.Add("Result", 360);

        Controls.Add(_list);
    }

    public void Append(string action, string result)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Append(action, result));
            return;
        }

        var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
        item.SubItems.Add(action);
        item.SubItems.Add(result);
        _list.Items.Add(item);
        item.EnsureVisible();
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
