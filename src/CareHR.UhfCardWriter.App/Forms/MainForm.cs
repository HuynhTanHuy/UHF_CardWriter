using System.Diagnostics;
using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Controls;
using CareHR.UhfCardWriter.App.Diagnostics;
using CareHR.UhfCardWriter.App.Presentation;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;
using CareHR.UhfCardWriter.Application.Services;
using Microsoft.Extensions.Options;

namespace CareHR.UhfCardWriter.App.Forms;

/// <summary>
/// Main CareHR UHF card writer window. Presentation only — calls Application Services.
/// </summary>
public sealed partial class MainForm : Form
{
    private readonly CardConnectionService _connectionService;
    private readonly CardScanningService _scanningService;
    private readonly CardWriteOrchestrator _orchestrator;
    private readonly AppSettings _settings;
    private readonly Dictionary<string, string> _timings = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _operationCts;
    private bool _busy;
    private UiState _uiState = UiState.Disconnected;

    public MainForm(
        CardConnectionService connectionService,
        CardScanningService scanningService,
        CardWriteOrchestrator orchestrator,
        IOptions<AppSettings> options)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _scanningService = scanningService ?? throw new ArgumentNullException(nameof(scanningService));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        ArgumentNullException.ThrowIfNull(options);
        _settings = options.Value ?? throw new ArgumentNullException(nameof(options));

        InitializeComponent();
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;

        statusPanel.ApplyTheme(_settings.Theme);
        workflowProgress.ApplyTheme(_settings.Theme);

        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        if (Tag is long startupMs)
            RecordTiming("StartupMs", startupMs);

        cboHospital.DataSource = _settings.Hospitals.ToList();
        cboCardType.DataSource = _settings.CardTypes.ToList();
        txtBatch.Text = _settings.Card.DefaultBatchCode;
        txtStart.Text = "1";
        txtEnd.Text = "100";
        txtCurrent.Text = "1";
        cboHospital.SelectedIndexChanged += (_, _) => RefreshTargetEpcPreview();
        txtCurrent.TextChanged += (_, _) => RefreshTargetEpcPreview();
        RefreshTargetEpcPreview();
        RefreshReaders();
        SetUiState(UiState.Disconnected, "Connect a desk reader to begin.");
        LogOp("App", "CareHR UHF Card Writer ready.");

        ShowStartupFindings();
    }

    private void ShowStartupFindings()
    {
        var findings = ConfigurationValidator.Validate(_settings).ToList();
        if (!DiagnosticsInfo.NativeDllPresent)
            findings.Add(new ConfigurationValidator.Finding("NAT-DLL", "Error", "UHFPrimeReader.dll is missing next to the application."));

        foreach (var f in findings)
            LogOp("Config", $"[{f.Severity}] {f.Message}");

        var errors = findings.Where(f => f.Severity == "Error").ToList();
        var warnings = findings.Where(f => f.Severity == "Warning").ToList();
        if (errors.Count == 0 && warnings.Count == 0)
            return;

        var lines = errors.Concat(warnings).Take(8).Select(f => "• " + f.Message);
        var text = "Startup checks:\n\n" + string.Join("\n", lines)
                   + "\n\nOpen Settings for About / Health / Export diagnostics.";
        MessageBox.Show(this, text, "Startup validation", MessageBoxButtons.OK,
            errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _operationCts?.Cancel();
        try
        {
            if (_connectionService.IsConnected)
                _ = _connectionService.Disconnect();
        }
        catch (Exception ex)
        {
            AppLog.Warn("Shutdown", AppLog.SanitizeException(ex));
        }

        AppLog.Info("Shutdown", "Main form closing.");
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            BtnCancel_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.F5)
        {
            BtnConnect_Click(sender, e);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F6)
        {
            BtnScan_Click(sender, e);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F7)
        {
            BtnWrite_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.R)
        {
            BtnRefresh_Click(sender, e);
            e.Handled = true;
        }
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_busy)
            return;

        if (_connectionService.IsConnected)
        {
            SetBusy(true);
            var sw = Stopwatch.StartNew();
            try
            {
                var close = await Task.Run(() => _connectionService.Disconnect()).ConfigureAwait(true);
                sw.Stop();
                RecordTiming("DisconnectMs", sw.ElapsedMilliseconds);
                LogOp("Disconnect",
                    close.Success ? close.Message : UserMessage.ForDeviceOrOperation($"{close.ErrorCode}: {close.Message}"),
                    sw.ElapsedMilliseconds);
                SetUiState(UiState.Disconnected, "Reader disconnected.");
                btnConnect.Text = "Connect (F5)";
            }
            finally
            {
                SetBusy(false);
            }

            return;
        }

        if (!TryBuildEndpoint(out var endpoint, out var error))
        {
            MessageBox.Show(this, error, "Connect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        SetUiState(UiState.Busy, "Connecting…");
        var connectSw = Stopwatch.StartNew();
        try
        {
            var result = await Task.Run(() => _connectionService.Connect(endpoint!)).ConfigureAwait(true);
            connectSw.Stop();
            RecordTiming("ConnectMs", connectSw.ElapsedMilliseconds);
            if (!result.Success)
            {
                var msg = UserMessage.ForDeviceOrOperation($"{result.ErrorCode}: {result.Message}");
                LogOp("Connect", msg, connectSw.ElapsedMilliseconds);
                SetUiState(UiState.Error, msg);
                return;
            }

            LogOp("Connect", "Connected.", connectSw.ElapsedMilliseconds);
            btnConnect.Text = "Disconnect";
            SetUiState(UiState.Connected, "Reader connected. Scan or write a card.");
        }
        catch (Exception ex)
        {
            connectSw.Stop();
            var msg = UserMessage.ForException(ex);
            AppLog.Error("Presentation", "Connect failed", ex);
            LogOp("Connect", msg, connectSw.ElapsedMilliseconds);
            SetUiState(UiState.Error, msg);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnScan_Click(object? sender, EventArgs e)
    {
        if (_busy)
            return;

        if (!_connectionService.IsConnected)
        {
            MessageBox.Show(this, "Connect the reader first.", "Scan", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _operationCts = new CancellationTokenSource();
        SetBusy(true);
        SetUiState(UiState.Scanning, "Scanning for a single card…");
        var sw = Stopwatch.StartNew();
        try
        {
            var timeout = _settings.Reader.ScanTimeoutMs;
            var scan = await Task.Run(
                    () => _scanningService.ScanForSingleCard(timeout, _operationCts.Token),
                    _operationCts.Token)
                .ConfigureAwait(true);
            sw.Stop();
            RecordTiming("ScanMs", sw.ElapsedMilliseconds);

            if (scan.Outcome == ScanOutcome.Cancelled)
            {
                LogOp("Scan", "Cancelled.", sw.ElapsedMilliseconds);
                SetUiState(UiState.Connected, "Scan cancelled.");
                return;
            }

            if (!scan.Success || scan.Card is null)
            {
                var msg = UserMessage.ForDeviceOrOperation($"{scan.ErrorCode}: {scan.Message}");
                LogOp("Scan", msg, sw.ElapsedMilliseconds);
                SetUiState(UiState.Error, msg);
                return;
            }

            var epc = scan.Card.Identity.EpcHex;
            txtCurrentEpc.Text = epc;
            lblResultCurrentEpc.Text = epc;
            LogOp("Scan", $"Card found: {epc}", sw.ElapsedMilliseconds);
            SetUiState(UiState.Connected, $"Card detected: {epc}");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            LogOp("Scan", "Cancelled.", sw.ElapsedMilliseconds);
            SetUiState(UiState.Connected, "Scan cancelled.");
        }
        catch (Exception ex)
        {
            sw.Stop();
            var msg = UserMessage.ForException(ex);
            AppLog.Error("Presentation", "Scan failed", ex);
            LogOp("Scan", msg, sw.ElapsedMilliseconds);
            SetUiState(UiState.Error, msg);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnWrite_Click(object? sender, EventArgs e)
    {
        if (_busy)
            return;

        if (!_connectionService.IsConnected)
        {
            MessageBox.Show(this, "Connect the reader first.", "Write", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!TryValidateWriteInputs(out var intendedBytes, out var hospitalId, out var cardTypeId, out var batch, out var error))
        {
            MessageBox.Show(this, error, "Write", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _operationCts = new CancellationTokenSource();
        SetBusy(true);
        SetUiState(UiState.Writing, "Writing card…");
        workflowProgress.SetActiveStep(2);

        var password = UiInputHelper.ResolveAccessPassword(_settings.Card);
        var request = new CardWriteJobRequest(
            new CardIdentity(intendedBytes!),
            password,
            hospitalId!,
            cardTypeId!,
            batch!,
            _settings.Reader.ScanTimeoutMs);

        lblResultTargetEpc.Text = Convert.ToHexString(intendedBytes!);
        lblResultHospital.Text = (cboHospital.SelectedItem as HospitalOption)?.Name ?? "—";
        lblResultCardType.Text = (cboCardType.SelectedItem as CardTypeOption)?.Name ?? "—";
        lblResultSerial.Text = txtCurrent.Text.Trim();

        var sw = Stopwatch.StartNew();
        try
        {
            LogOp("Write", "Job started (scan → write → verify → register).");
            var result = await Task.Run(
                    () => _orchestrator.RunWriteCardJob(request, _operationCts.Token),
                    _operationCts.Token)
                .ConfigureAwait(true);
            sw.Stop();
            RecordTiming("WriteJobMs", sw.ElapsedMilliseconds);
            if (result.RegistrationResult is not null)
                RecordTiming("RegisterOutcome", result.RegistrationResult.Success ? "OK" : "FAIL");
            ApplyJobResult(result, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            LogOp("Cancel", "Write cancelled.", sw.ElapsedMilliseconds);
            SetUiState(UiState.Connected, "Write cancelled.");
        }
        catch (Exception ex)
        {
            sw.Stop();
            var msg = UserMessage.ForException(ex);
            AppLog.Error("Presentation", "Write job failed", ex);
            LogOp("Error", msg, sw.ElapsedMilliseconds);
            SetUiState(UiState.Error, msg);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        _operationCts?.Cancel();
        try
        {
            var cancel = _orchestrator.CancelOperation();
            LogOp("Cancel", UserMessage.ForDeviceOrOperation(cancel.Message));
        }
        catch (Exception ex)
        {
            LogOp("Cancel", UserMessage.ForException(ex));
        }
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        if (_busy)
            return;
        RefreshReaders();
        LogOp("Refresh", "Reader list reloaded.");
    }

    private void BtnSettings_Click(object? sender, EventArgs e)
    {
        using var dlg = new SupportForm(
            _settings,
            _connectionService,
            () => cboReader.Text,
            () => operationLog.GetLines(),
            _timings);
        dlg.ShowDialog(this);
    }

    private void ApplyJobResult(CardWriteJobResult result, long durationMs)
    {
        if (result.ScannedCard is not null)
        {
            txtCurrentEpc.Text = result.ScannedCard.Identity.EpcHex;
            lblResultCurrentEpc.Text = result.ScannedCard.Identity.EpcHex;
        }

        if (result.Success)
        {
            LogOp("Write", UserMessage.ForDeviceOrOperation(result.Message), durationMs);
            LogOp("Verify", "OK");
            LogOp("Register", "OK");
            SetUiState(UiState.Completed, result.Message);
            if (UiInputHelper.TryParsePositiveInt(txtCurrent.Text, out var current) &&
                UiInputHelper.TryParsePositiveInt(txtEnd.Text, out var end) &&
                current < end)
            {
                txtCurrent.Text = (current + 1).ToString();
                RefreshTargetEpcPreview();
            }

            return;
        }

        if (result.Stage == CardWriteJobStage.WrittenButUnregistered)
        {
            var regMsg = UserMessage.ForDeviceOrOperation(result.Message);
            LogOp("Verify", "OK", durationMs);
            LogOp("Register", regMsg);
            SetUiState(UiState.Error, "Card written & verified, but registry failed. See Register log / Support.");
            workflowProgress.SetActiveStep(4);
            return;
        }

        if (result.Stage == CardWriteJobStage.Cancelled)
        {
            LogOp("Cancel", "Cancelled.", durationMs);
            SetUiState(UiState.Connected, "Cancelled.");
            return;
        }

        var fail = UserMessage.ForDeviceOrOperation($"{result.Stage}: {result.ErrorCode} — {result.Message}");
        LogOp("Error", fail, durationMs);
        workflowProgress.SetActiveStep(result.Stage switch
        {
            CardWriteJobStage.Scanning => 1,
            CardWriteJobStage.Selecting => 1,
            CardWriteJobStage.Writing => 2,
            CardWriteJobStage.Verifying => 3,
            CardWriteJobStage.Registering => 4,
            _ => 2,
        });
        SetUiState(UiState.Error, UserMessage.ForDeviceOrOperation(result.Message));
    }

    private void LogOp(string action, string result, long? durationMs = null)
    {
        operationLog.Append(action, result, durationMs);
        AppLog.Operation(action, result, durationMs);
    }

    private void RecordTiming(string key, long ms) => _timings[key] = ms + " ms";

    private void RecordTiming(string key, string value) => _timings[key] = value;


    private void RefreshReaders()
    {
        var previous = cboReader.SelectedItem;
        var items = new List<ReaderListItem>
        {
            new($"Serial ({_settings.Reader.ComPort})", ReaderEndpoint.Serial(_settings.Reader.ComPort, _settings.Reader.BaudRate)),
            new(
                $"Network ({_settings.Reader.NetworkIp}:{_settings.Reader.NetworkPort})",
                ReaderEndpoint.Network(_settings.Reader.NetworkIp, _settings.Reader.NetworkPort, _settings.Reader.NetworkTimeoutMs)),
        };

        var usb = _connectionService.ListUsbReaders();
        if (usb.Success && usb.Value is not null)
        {
            foreach (var r in usb.Value)
                items.Add(new ReaderListItem($"USB [{r.Index}] {r.DisplayName}", ReaderEndpoint.UsbHid(r.Index)));
        }
        else if (!usb.Success)
        {
            LogOp("Refresh", UserMessage.ForDeviceOrOperation($"USB list: {usb.ErrorCode} {usb.Message}"));
        }

        cboReader.DataSource = null;
        cboReader.DisplayMember = nameof(ReaderListItem.Display);
        cboReader.DataSource = items;

        if (previous is ReaderListItem prev)
        {
            var match = items.Find(i => i.Display == prev.Display);
            if (match is not null)
                cboReader.SelectedItem = match;
        }
        else
        {
            var mode = _settings.Reader.DefaultMode;
            if (string.Equals(mode, "Serial", StringComparison.OrdinalIgnoreCase))
                cboReader.SelectedIndex = 0;
            else if (string.Equals(mode, "Network", StringComparison.OrdinalIgnoreCase))
                cboReader.SelectedIndex = 1;
            else if (items.Count > 2)
                cboReader.SelectedIndex = 2;
        }

        lblResultReader.Text = cboReader.Text;
    }

    private void RefreshTargetEpcPreview()
    {
        if (string.Equals(_settings.Card.EpcEncoding, "Hex", StringComparison.OrdinalIgnoreCase))
            return;

        var hospital = cboHospital.SelectedItem as HospitalOption;
        if (!UiInputHelper.TryParsePositiveInt(txtCurrent.Text, out var serial))
            return;

        var preview = UiInputHelper.BuildAsciiEpcPreview(
            hospital?.Code ?? string.Empty,
            serial,
            _settings.Card.SerialPadWidth);
        txtTargetEpc.Text = Convert.ToHexString(UiInputHelper.AsciiToEpcBytes(preview));
        lblResultTargetEpc.Text = txtTargetEpc.Text;
    }

    private bool TryBuildEndpoint(out ReaderEndpoint? endpoint, out string error)
    {
        endpoint = null;
        error = string.Empty;
        if (cboReader.SelectedItem is not ReaderListItem item)
        {
            error = "Select a reader.";
            return false;
        }

        endpoint = item.Endpoint;
        return true;
    }

    private bool TryValidateWriteInputs(
        out byte[]? epc,
        out string? hospitalId,
        out string? cardTypeId,
        out string? batch,
        out string error)
    {
        epc = null;
        hospitalId = null;
        cardTypeId = null;
        batch = null;
        error = string.Empty;

        if (cboHospital.SelectedItem is not HospitalOption hospital || string.IsNullOrWhiteSpace(hospital.Id))
        {
            error = "Hospital is required.";
            return false;
        }

        if (cboCardType.SelectedItem is not CardTypeOption type || string.IsNullOrWhiteSpace(type.Id))
        {
            error = "Card type is required.";
            return false;
        }

        batch = txtBatch.Text.Trim();
        if (string.IsNullOrWhiteSpace(batch))
        {
            error = "Batch code is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtTargetEpc.Text))
        {
            error = "Target EPC is required.";
            return false;
        }

        if (!UiInputHelper.TryParseHexBytes(txtTargetEpc.Text, out var bytes, out error))
            return false;

        if (!UiInputHelper.TryParsePositiveInt(txtStart.Text, out _) ||
            !UiInputHelper.TryParsePositiveInt(txtEnd.Text, out _) ||
            !UiInputHelper.TryParsePositiveInt(txtCurrent.Text, out _))
        {
            error = "Start / End / Current must be numbers.";
            return false;
        }

        epc = bytes;
        hospitalId = hospital.Id;
        cardTypeId = type.Id;
        return true;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        var enableInputs = !busy;
        cboReader.Enabled = enableInputs;
        cboHospital.Enabled = enableInputs;
        cboCardType.Enabled = enableInputs;
        txtStart.Enabled = enableInputs;
        txtEnd.Enabled = enableInputs;
        txtCurrent.Enabled = enableInputs;
        txtTargetEpc.Enabled = enableInputs && string.Equals(_settings.Card.EpcEncoding, "Hex", StringComparison.OrdinalIgnoreCase);
        txtBatch.Enabled = enableInputs;
        btnConnect.Enabled = !busy;
        btnScan.Enabled = !busy;
        btnWrite.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        btnSettings.Enabled = !busy;
        btnCancel.Enabled = true;
        UseWaitCursor = busy;
    }

    private void SetUiState(UiState state, string detail)
    {
        _uiState = state;
        statusPanel.SetState(state, detail);
        workflowProgress.SetFromUiState(state);
        lblResultReader.Text = cboReader.Text;
    }

    private sealed class ReaderListItem
    {
        public ReaderListItem(string display, ReaderEndpoint endpoint)
        {
            Display = display;
            Endpoint = endpoint;
        }

        public string Display { get; }
        public ReaderEndpoint Endpoint { get; }
        public override string ToString() => Display;
    }
}
