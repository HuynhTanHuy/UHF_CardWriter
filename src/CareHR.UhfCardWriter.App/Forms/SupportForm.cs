using System.Diagnostics;
using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Controls;
using CareHR.UhfCardWriter.App.Diagnostics;
using CareHR.UhfCardWriter.Application.Services;

namespace CareHR.UhfCardWriter.App.Forms;

internal sealed class SupportForm : Form
{
    private readonly AppSettings _settings;
    private readonly CardConnectionService _connection;
    private readonly Func<string?> _readerLabel;
    private readonly Func<IEnumerable<string>> _operationLines;
    private readonly IReadOnlyDictionary<string, string> _timings;
    private readonly Func<bool> _getDebugMode;
    private readonly Action<bool> _setDebugMode;
    private readonly TextBox _info;
    private readonly ListView _health;
    private readonly CheckBox _chkDebug;

    public SupportForm(
        AppSettings settings,
        CardConnectionService connection,
        Func<string?> readerLabel,
        Func<IEnumerable<string>> operationLines,
        IReadOnlyDictionary<string, string> timings,
        Func<bool> getDebugMode,
        Action<bool> setDebugMode)
    {
        _settings = settings;
        _connection = connection;
        _readerLabel = readerLabel;
        _operationLines = operationLines;
        _timings = timings;
        _getDebugMode = getDebugMode ?? (() => false);
        _setDebugMode = setDebugMode ?? (_ => { });

        Text = "About & Support";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        ClientSize = new Size(640, 560);
        MinimumSize = new Size(640, 520);
        Font = new Font("Segoe UI", 9f);
        Padding = new Padding(0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 8, 8, 8),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var aboutPage = new TabPage("About");
        var healthPage = new TabPage("Health");

        var aboutLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4),
        };
        aboutLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        aboutLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));

        _info = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9f),
            Text = DiagnosticsInfo.Summarize(settings, connection.IsConnected, readerLabel()),
        };

        _chkDebug = new CheckBox
        {
            Text = "Debug Mode (show EPC)",
            AutoSize = true,
            Dock = DockStyle.Left,
            Checked = _getDebugMode(),
            Margin = new Padding(0, 6, 0, 0),
        };
        _chkDebug.CheckedChanged += (_, _) => _setDebugMode(_chkDebug.Checked);

        aboutLayout.Controls.Add(_info, 0, 0);
        aboutLayout.Controls.Add(_chkDebug, 0, 1);
        aboutPage.Controls.Add(aboutLayout);

        _health = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9f),
        };
        _health.Columns.Add("Status", 70);
        _health.Columns.Add("Check", 180);
        _health.Columns.Add("Detail", 340);
        healthPage.Controls.Add(_health);

        tabs.TabPages.Add(aboutPage);
        tabs.TabPages.Add(healthPage);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0),
        };

        var btnClose = new Button
        {
            Text = "Close",
            Width = 100,
            Height = 32,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0, 0, 0, 0),
        };
        var btnExport = new Button
        {
            Text = "Export diagnostics…",
            Width = 150,
            Height = 32,
            AutoSize = false,
            Margin = new Padding(8, 0, 0, 0),
        };
        var btnOpenLogs = new Button
        {
            Text = "Open log folder",
            Width = 130,
            Height = 32,
            Margin = new Padding(8, 0, 0, 0),
        };
        var btnRefresh = new Button
        {
            Text = "Refresh",
            Width = 100,
            Height = 32,
            Margin = new Padding(8, 0, 0, 0),
        };

        btnExport.Click += (_, _) => Export();
        btnOpenLogs.Click += (_, _) => OpenLogs();
        btnRefresh.Click += (_, _) => RefreshAll();
        btnClose.Click += (_, _) => Close();

        buttons.Controls.Add(btnClose);
        buttons.Controls.Add(btnExport);
        buttons.Controls.Add(btnOpenLogs);
        buttons.Controls.Add(btnRefresh);

        root.Controls.Add(tabs, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        Controls.Add(root);
        CancelButton = btnClose;

        Load += (_, _) => RefreshAll();
    }

    private void RefreshAll()
    {
        _info.Text = DiagnosticsInfo.Summarize(_settings, _connection.IsConnected, _readerLabel());
        _health.Items.Clear();
        foreach (var c in HealthChecker.Run(_settings, _connection, _readerLabel()))
        {
            var item = new ListViewItem(c.Ready ? "OK" : "FAIL");
            item.SubItems.Add(c.Name);
            item.SubItems.Add(c.Detail);
            if (!c.Ready)
                item.ForeColor = Color.DarkRed;
            _health.Items.Add(item);
        }
    }

    private void Export()
    {
        try
        {
            var path = DiagnosticsExporter.Export(
                _settings,
                _connection,
                _readerLabel(),
                _operationLines(),
                _timings);
            MessageBox.Show(this,
                "Diagnostics exported:\n" + path,
                "Export",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            }
            catch
            {
                // Ignore explorer failures.
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UserMessage.ForException(ex), "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenLogs()
    {
        try
        {
            AppPaths.EnsureCreated();
            Process.Start(new ProcessStartInfo("explorer.exe", AppPaths.Logs) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UserMessage.ForException(ex), "Logs", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
