using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Controls;
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
        operationLog.Append("App", "CareHR UHF Card Writer ready.");
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _operationCts?.Cancel();
        try
        {
            if (_connectionService.IsConnected)
                _ = _connectionService.Disconnect();
        }
        catch
        {
            // Best-effort close
        }
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
            try
            {
                var close = await Task.Run(() => _connectionService.Disconnect()).ConfigureAwait(true);
                operationLog.Append("Disconnect", close.Success ? close.Message : $"{close.ErrorCode}: {close.Message}");
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
        try
        {
            var result = await Task.Run(() => _connectionService.Connect(endpoint!)).ConfigureAwait(true);
            if (!result.Success)
            {
                operationLog.Append("Connect", $"{result.ErrorCode}: {result.Message}");
                SetUiState(UiState.Error, result.Message);
                return;
            }

            operationLog.Append("Connect", "Connected.");
            btnConnect.Text = "Disconnect";
            SetUiState(UiState.Connected, "Reader connected. Scan or write a card.");
        }
        catch (Exception ex)
        {
            operationLog.Append("Connect", ex.Message);
            SetUiState(UiState.Error, ex.Message);
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
        try
        {
            var timeout = _settings.Reader.ScanTimeoutMs;
            var scan = await Task.Run(
                    () => _scanningService.ScanForSingleCard(timeout, _operationCts.Token),
                    _operationCts.Token)
                .ConfigureAwait(true);

            if (scan.Outcome == ScanOutcome.Cancelled)
            {
                operationLog.Append("Scan", "Cancelled.");
                SetUiState(UiState.Connected, "Scan cancelled.");
                return;
            }

            if (!scan.Success || scan.Card is null)
            {
                operationLog.Append("Scan", $"{scan.ErrorCode}: {scan.Message}");
                SetUiState(UiState.Error, scan.Message);
                return;
            }

            var epc = scan.Card.Identity.EpcHex;
            txtCurrentEpc.Text = epc;
            lblResultCurrentEpc.Text = epc;
            operationLog.Append("Scan", $"Card found: {epc}");
            SetUiState(UiState.Connected, $"Card detected: {epc}");
        }
        catch (OperationCanceledException)
        {
            operationLog.Append("Scan", "Cancelled.");
            SetUiState(UiState.Connected, "Scan cancelled.");
        }
        catch (Exception ex)
        {
            operationLog.Append("Scan", ex.Message);
            SetUiState(UiState.Error, ex.Message);
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

        if (!TryValidateWriteInputs(out var intendedBytes, out var cardTypeId, out var batch, out var error))
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
            cardTypeId!,
            batch!,
            _settings.Reader.ScanTimeoutMs);

        lblResultTargetEpc.Text = Convert.ToHexString(intendedBytes!);
        lblResultHospital.Text = (cboHospital.SelectedItem as HospitalOption)?.Name ?? "—";
        lblResultCardType.Text = (cboCardType.SelectedItem as CardTypeOption)?.Name ?? "—";
        lblResultSerial.Text = txtCurrent.Text.Trim();

        try
        {
            operationLog.Append("Write", "Job started (scan → write → verify → register).");
            var result = await Task.Run(
                    () => _orchestrator.RunWriteCardJob(request, _operationCts.Token),
                    _operationCts.Token)
                .ConfigureAwait(true);

            ApplyJobResult(result);
        }
        catch (OperationCanceledException)
        {
            operationLog.Append("Write", "Cancelled.");
            SetUiState(UiState.Connected, "Write cancelled.");
        }
        catch (Exception ex)
        {
            operationLog.Append("Write", ex.Message);
            SetUiState(UiState.Error, ex.Message);
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
            operationLog.Append("Cancel", cancel.Message);
        }
        catch (Exception ex)
        {
            operationLog.Append("Cancel", ex.Message);
        }
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        if (_busy)
            return;
        RefreshReaders();
        operationLog.Append("Refresh", "Reader list reloaded.");
    }

    private void BtnSettings_Click(object? sender, EventArgs e)
    {
        var message =
            $"API: {_settings.Api.BaseUrl}\n" +
            $"Reader mode: {_settings.Reader.DefaultMode}\n" +
            $"COM: {_settings.Reader.ComPort} @ {_settings.Reader.BaudRate}\n" +
            $"EPC encoding: {_settings.Card.EpcEncoding}\n\n" +
            "Edit appsettings.json next to the executable to change settings.";
        MessageBox.Show(this, message, "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ApplyJobResult(CardWriteJobResult result)
    {
        if (result.ScannedCard is not null)
        {
            txtCurrentEpc.Text = result.ScannedCard.Identity.EpcHex;
            lblResultCurrentEpc.Text = result.ScannedCard.Identity.EpcHex;
        }

        if (result.Success)
        {
            operationLog.Append("Write", result.Message);
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
            operationLog.Append("Register", result.Message);
            SetUiState(UiState.Error, "Card written & verified, but registry failed. Retry register from backend policy.");
            workflowProgress.SetActiveStep(4);
            return;
        }

        if (result.Stage == CardWriteJobStage.Cancelled)
        {
            operationLog.Append("Write", "Cancelled.");
            SetUiState(UiState.Connected, "Cancelled.");
            return;
        }

        operationLog.Append("Write", $"{result.Stage}: {result.ErrorCode} — {result.Message}");
        // Reflect Application stage (not fabricated mid-flight progress).
        workflowProgress.SetActiveStep(result.Stage switch
        {
            CardWriteJobStage.Scanning => 1,
            CardWriteJobStage.Selecting => 1,
            CardWriteJobStage.Writing => 2,
            CardWriteJobStage.Verifying => 3,
            CardWriteJobStage.Registering => 4,
            _ => 2,
        });
        SetUiState(UiState.Error, result.Message);
    }

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
            operationLog.Append("Refresh", $"USB list: {usb.ErrorCode} {usb.Message}");
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

    private bool TryValidateWriteInputs(out byte[]? epc, out string? cardTypeId, out string? batch, out string error)
    {
        epc = null;
        cardTypeId = null;
        batch = null;
        error = string.Empty;

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
