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
        Width = 640;
        Height = 520;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var aboutPage = new TabPage("About");
        var healthPage = new TabPage("Health");

        _info = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9f),
            Text = DiagnosticsInfo.Summarize(settings, connection.IsConnected, readerLabel()),
        };
        aboutPage.Controls.Add(_info);

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
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
        };

        var btnClose = new Button { Text = "Close", Width = 100, DialogResult = DialogResult.Cancel };
        var btnExport = new Button { Text = "Export diagnostics…", Width = 150, AutoSize = false };
        var btnOpenLogs = new Button { Text = "Open log folder", Width = 130 };
        var btnRefresh = new Button { Text = "Refresh", Width = 100 };
        _chkDebug = new CheckBox
        {
            Text = "Debug Mode (show EPC)",
            AutoSize = true,
            Checked = _getDebugMode(),
            Margin = new Padding(8, 10, 16, 0),
        };
        _chkDebug.CheckedChanged += (_, _) => _setDebugMode(_chkDebug.Checked);

        btnExport.Click += (_, _) => Export();
        btnOpenLogs.Click += (_, _) => OpenLogs();
        btnRefresh.Click += (_, _) => RefreshAll();
        btnClose.Click += (_, _) => Close();

        buttons.Controls.Add(btnClose);
        buttons.Controls.Add(btnExport);
        buttons.Controls.Add(btnOpenLogs);
        buttons.Controls.Add(btnRefresh);
        buttons.Controls.Add(_chkDebug);

        Controls.Add(tabs);
        Controls.Add(buttons);
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
