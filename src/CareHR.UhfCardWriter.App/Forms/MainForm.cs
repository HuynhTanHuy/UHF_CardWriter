using System.Diagnostics;
using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Diagnostics;
using CareHR.UhfCardWriter.App.Presentation;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;
using CareHR.UhfCardWriter.Application.Services;
using Microsoft.Extensions.Options;

namespace CareHR.UhfCardWriter.App.Forms;

/// <summary>
/// Main CareHR UHF card writer window — batch provisioning Presentation only.
/// </summary>
public sealed partial class MainForm : Form
{
    private readonly CardConnectionService _connectionService;
    private readonly CardScanningService _scanningService;
    private readonly CardWriteOrchestrator _orchestrator;
    private readonly AppSettings _settings;
    private readonly Dictionary<string, string> _timings = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _batchCts;
    private bool _batchRunning;
    private bool _busy;
    private bool _debugMode;
    private UiState _uiState = UiState.Disconnected;

    private int _sessionWritten;
    private int _sessionSuccess;
    private int _sessionFailed;
    private Stopwatch? _batchElapsed;
    private System.Windows.Forms.Timer? _elapsedTimer;

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

        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        if (Tag is long startupMs)
            RecordTiming("StartupMs", startupMs);

        cboHospital.DataSource = _settings.Hospitals.ToList();
        cboCardType.DataSource = _settings.CardTypes.ToList();
        txtBatch.Text = _settings.Card.DefaultBatchNumber.ToString();
        txtStart.Text = "1";
        txtEnd.Text = "100";
        txtCurrent.Text = "1";

        txtStart.TextChanged += (_, _) =>
        {
            if (_batchRunning)
                return;
            SyncCurrentFromStart();
            RefreshTargetCardPreview();
            RefreshBatchCounters();
        };
        txtEnd.TextChanged += (_, _) =>
        {
            if (!_batchRunning)
                RefreshBatchCounters();
        };
        txtBatch.TextChanged += (_, _) =>
        {
            if (!_batchRunning)
                RefreshTargetCardPreview();
        };
        cboHospital.SelectedIndexChanged += (_, _) => RefreshTargetCardPreview();
        txtCurrent.TextChanged += (_, _) =>
        {
            if (!_batchRunning)
                RefreshTargetCardPreview();
        };

        RefreshReaders();
        RefreshTargetCardPreview();
        RefreshBatchCounters();
        ApplyDebugVisibility();
        RefreshConnectionChrome();
        SetUiState(UiState.Disconnected, "Connect a desk reader to begin.");
        LogOp("Ready", "Application ready.");
        ShowStartupFindings();
    }

    private void ShowStartupFindings()
    {
        var findings = ConfigurationValidator.Validate(_settings).ToList();
        if (!DiagnosticsInfo.NativeDllPresent)
            findings.Add(new ConfigurationValidator.Finding("NAT-DLL", "Error", "UHFPrimeReader.dll is missing next to the application."));

        var errors = findings.Where(f => f.Severity == "Error").ToList();
        var warnings = findings.Where(f => f.Severity == "Warning").ToList();
        if (errors.Count == 0 && warnings.Count == 0)
            return;

        foreach (var f in errors)
            LogOp("Setup", OperatorFriendlyConfig(f.Message));

        var lines = errors.Concat(warnings).Take(8).Select(f => "• " + OperatorFriendlyConfig(f.Message));
        var text = "Startup checks:\n\n" + string.Join("\n", lines)
                   + "\n\nOpen Settings for About / Health / Export diagnostics.";
        MessageBox.Show(this, text, "Startup validation", MessageBoxButtons.OK,
            errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private static string OperatorFriendlyConfig(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Configuration issue.";
        if (message.Contains("UHFPrimeReader", StringComparison.OrdinalIgnoreCase))
            return "Reader driver is missing. Contact IT.";
        if (message.Contains("BaseUrl", StringComparison.OrdinalIgnoreCase))
            return "API address is not configured.";
        if (message.Contains("BearerToken", StringComparison.OrdinalIgnoreCase))
            return "API login token is not configured.";
        return UserMessage.SafeMessage(message);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _batchCts?.Cancel();
        _elapsedTimer?.Stop();
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
            BtnStop_Click(sender, e);
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
            BtnStart_Click(sender, e);
            e.Handled = true;
        }
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_busy || _batchRunning)
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
                LogOp("Disconnect", close.Success
                    ? "Reader disconnected."
                    : UserMessage.ForDeviceOrOperation(close.Message));
                SetConnectButtonText(connected: false);
                SetUiState(UiState.Disconnected, "Reader disconnected.");
                RefreshConnectionChrome();
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
                var msg = UserMessage.ForDeviceOrOperation(result.Message);
                LogOp("Connect", msg);
                SetUiState(UiState.Failed, msg);
                return;
            }

            LogOp("Connect", "Reader connected.");
            SetConnectButtonText(connected: true);
            SetUiState(UiState.Ready, "Reader connected. Set range and press Start.");
            RefreshConnectionChrome();
        }
        catch (Exception ex)
        {
            connectSw.Stop();
            var msg = UserMessage.ForException(ex);
            AppLog.Error("Presentation", "Connect failed", ex);
            LogOp("Connect", msg);
            SetUiState(UiState.Failed, msg);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_busy || _batchRunning)
            return;

        if (!_connectionService.IsConnected)
        {
            MessageBox.Show(this, "Connect the reader first.", "Start", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!TryParseRange(out var start, out var end, out var current, out var error))
        {
            MessageBox.Show(this, error, "Start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (current > end)
        {
            current = start;
            txtCurrent.Text = current.ToString();
        }

        if (!TryValidateBatchInputs(out _, out _, out _, out _, out error))
        {
            MessageBox.Show(this, error, "Start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _batchCts = new CancellationTokenSource();
        _batchRunning = true;
        _sessionWritten = 0;
        _sessionSuccess = 0;
        _sessionFailed = 0;
        _batchElapsed = Stopwatch.StartNew();
        StartElapsedTimer();
        SetBatchBusy(true);

        var total = end - start + 1;
        batchResult.ResetSession(current, Math.Max(0, end - current + 1), start, end);
        RefreshBatchCounters();
        LogOp("Batch", $"Started. Cards {start}–{end}, from {current}.");

        try
        {
            while (!_batchCts.IsCancellationRequested)
            {
                if (!UiInputHelper.TryParsePositiveInt(txtCurrent.Text, out current))
                {
                    FailBatchStop("Current number is invalid.", CardWriteJobStage.Failed);
                    break;
                }

                if (!UiInputHelper.TryParsePositiveInt(txtEnd.Text, out end))
                {
                    FailBatchStop("End number is invalid.", CardWriteJobStage.Failed);
                    break;
                }

                if (current > end)
                {
                    SetUiState(UiState.Done, $"Batch complete. Written {_sessionSuccess} card(s).");
                    LogOp("Batch", "Completed.");
                    PlaySuccessBeep();
                    break;
                }

                if (!TryBuildCardIdentity(current, out var cardNumber, out var intendedIdentity, out var hospitalId, out var cardTypeId, out var batchCode, out var buildError))
                {
                    FailBatchStop(buildError, CardWriteJobStage.Failed);
                    break;
                }

                lblFactoryEpc.Text = "—";
                var cardDisplay = FormatCardDisplay(cardNumber!);
                lblTargetCard.Text = cardDisplay;
                SetUiState(UiState.WaitingForCard, $"Place card for {cardDisplay}.");
                RefreshBatchCounters();

                // Scan first so operator sees Factory Card before write.
                ScanResult scan;
                try
                {
                    scan = await Task.Run(
                            () => _scanningService.ScanForSingleCard(_settings.Reader.ScanTimeoutMs, _batchCts.Token),
                            _batchCts.Token)
                        .ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    LogOp("Stop", "Batch stopped.");
                    SetUiState(UiState.Ready, "Batch stopped.");
                    break;
                }

                if (_batchCts.IsCancellationRequested || scan.Outcome == ScanOutcome.Cancelled)
                {
                    LogOp("Stop", "Batch stopped.");
                    SetUiState(UiState.Ready, "Batch stopped.");
                    break;
                }

                if (!scan.Success || scan.Card is null)
                {
                    SetUiState(UiState.WaitingForCard, $"Place card for {cardDisplay}.");
                    continue;
                }

                LogOp("Scan", "Card detected.");
                lblFactoryEpc.Text = scan.Card.Identity.EpcHex;
                if (_debugMode)
                {
                    txtCurrentEpc.Text = scan.Card.Identity.EpcHex;
                    txtTargetEpc.Text = Convert.ToHexString(intendedIdentity!.Epc);
                }

                var password = UiInputHelper.ResolveAccessPassword(_settings.Card);
                var request = new CardWriteJobRequest(
                    intendedIdentity!,
                    password,
                    hospitalId!,
                    cardTypeId!,
                    batchCode!,
                    _settings.Reader.ScanTimeoutMs);

                SetUiState(UiState.Writing, $"Writing {cardDisplay}…");
                LogOp("Write", "Writing.");

                CardWriteJobResult result;
                try
                {
                    var scannedCard = scan.Card;
                    result = await Task.Run(
                            () => _orchestrator.RunWriteCardJob(request, _batchCts.Token, scannedCard),
                            _batchCts.Token)
                        .ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    LogOp("Stop", "Batch stopped.");
                    SetUiState(UiState.Ready, "Batch stopped.");
                    break;
                }

                if (_batchCts.IsCancellationRequested || result.Stage == CardWriteJobStage.Cancelled)
                {
                    LogOp("Stop", "Batch stopped.");
                    SetUiState(UiState.Ready, "Batch stopped.");
                    break;
                }

                if (result.Success)
                {
                    _sessionSuccess++;
                    _sessionWritten++;

                    LogOp("Verify", "Verify OK.");
                    LogOp("Register", "Register OK.");
                    LogOp("Done", $"Completed {cardDisplay}.");
                    SetUiState(UiState.Success, $"{cardDisplay} written & registered.");
                    PlaySuccessBeep();

                    current++;
                    txtCurrent.Text = current.ToString();
                    RefreshTargetCardPreview();
                    RefreshBatchCounters();

                    await Task.Delay(400).ConfigureAwait(true);
                    if (!_batchCts.IsCancellationRequested && current <= end)
                        SetUiState(UiState.Ready, "Remove card, then place the next card.");
                    continue;
                }

                // Fail policy: Write / Verify / Register / other → stop, do not increment Current.
                _sessionFailed++;
                RefreshBatchCounters();
                var failMsg = MapStageFailure(result);
                LogOp("Fail", failMsg);
                SetUiState(UiState.Failed, failMsg);
                PlayFailBeep();
                LogOp("Batch", $"Stopped at {txtCurrent.Text}. Number not skipped.");
                break;
            }
        }
        catch (Exception ex)
        {
            _sessionFailed++;
            RefreshBatchCounters();
            var msg = UserMessage.ForException(ex);
            AppLog.Error("Presentation", "Batch failed", ex);
            LogOp("Fail", msg);
            SetUiState(UiState.Failed, msg);
            PlayFailBeep();
        }
        finally
        {
            _batchRunning = false;
            _batchElapsed?.Stop();
            StopElapsedTimer();
            SetBatchBusy(false);
            if (_connectionService.IsConnected &&
                _uiState is not UiState.Failed and not UiState.Done)
            {
                SetUiState(UiState.Ready, "Ready for next card. Press Start to continue.");
            }
        }
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        if (!_batchRunning && _batchCts is null)
            return;

        _batchCts?.Cancel();
        try
        {
            _ = _orchestrator.CancelOperation();
            LogOp("Stop", "Stopping…");
        }
        catch (Exception ex)
        {
            LogOp("Stop", UserMessage.ForException(ex));
        }
    }

    private void BtnSettings_Click(object? sender, EventArgs e)
    {
        if (_batchRunning)
            return;

        using var dlg = new SupportForm(
            _settings,
            _connectionService,
            () => cboReader.Text,
            () => operationLog.GetLines(),
            _timings,
            () => _debugMode,
            value =>
            {
                _debugMode = value;
                ApplyDebugVisibility();
            });
        dlg.ShowDialog(this);
        RefreshReaders();
    }

    private void FailBatchStop(string message, CardWriteJobStage stage)
    {
        _sessionFailed++;
        RefreshBatchCounters();
        var msg = MapStageFailureMessage(stage, message);
        LogOp("Fail", msg);
        SetUiState(UiState.Failed, msg);
        PlayFailBeep();
    }

    private static string MapStageFailure(CardWriteJobResult result) =>
        MapStageFailureMessage(result.Stage, result.Message);

    private static string MapStageFailureMessage(CardWriteJobStage stage, string? message)
    {
        var safe = UserMessage.ForDeviceOrOperation(message);
        return stage switch
        {
            CardWriteJobStage.Writing => "Writing failed. " + safe,
            CardWriteJobStage.Verifying => "Verify failed. " + safe,
            CardWriteJobStage.Registering or CardWriteJobStage.WrittenButUnregistered => "Register failed. " + safe,
            CardWriteJobStage.Scanning => "Card not detected. Place the card again.",
            CardWriteJobStage.Selecting => "Could not select card. Place the card again.",
            _ => safe,
        };
    }

    private void SetConnectButtonText(bool connected)
    {
        btnConnect.Text = connected ? "Disconnect" : "Connect";
    }

    private void SyncCurrentFromStart()
    {
        if (UiInputHelper.TryParsePositiveInt(txtStart.Text, out var start))
            txtCurrent.Text = start.ToString();
    }

    private void ApplyDebugVisibility()
    {
        SetDebugRowVisible(_debugMode);
        if (!_debugMode)
        {
            txtTargetEpc.Text = string.Empty;
            txtCurrentEpc.Text = string.Empty;
        }
        else
        {
            RefreshTargetCardPreview();
        }
    }

    private void RefreshTargetCardPreview()
    {
        if (!UiInputHelper.TryParsePositiveInt(txtCurrent.Text, out var current))
        {
            lblTargetCard.Text = "—";
            return;
        }

        if (!TryBuildCardIdentity(current, out var cardNumber, out var identity, out _, out _, out _, out _))
        {
            lblTargetCard.Text = "—";
            return;
        }

        lblTargetCard.Text = FormatCardDisplay(cardNumber!);
        if (_debugMode && identity is not null)
            txtTargetEpc.Text = Convert.ToHexString(identity.Epc);
    }

    private string FormatCardDisplay(string cardNumber) =>
        UiCardNumberFormat.ForDisplay(
            cardNumber,
            _settings.Card.BatchNumberWidth,
            _settings.Card.SerialNumberWidth);

    private bool TryValidateBatchInputs(
        out string? hospitalId,
        out string? cardTypeId,
        out string? batch,
        out byte[]? sampleEpc,
        out string error)
    {
        hospitalId = null;
        cardTypeId = null;
        batch = null;
        sampleEpc = null;
        error = string.Empty;

        if (!UiInputHelper.TryParsePositiveInt(txtCurrent.Text, out var current))
        {
            error = "Current number is invalid.";
            return false;
        }

        if (!TryBuildCardIdentity(current, out _, out var identity, out hospitalId, out cardTypeId, out batch, out error))
            return false;

        sampleEpc = identity!.Epc;
        return true;
    }

    private bool TryBuildCardIdentity(
        int serial,
        out string? cardNumber,
        out CardIdentity? identity,
        out string? hospitalId,
        out string? cardTypeId,
        out string? batchCode,
        out string error)
    {
        cardNumber = null;
        identity = null;
        hospitalId = null;
        cardTypeId = null;
        batchCode = null;
        error = string.Empty;

        if (cboHospital.SelectedItem is not HospitalOption hospital || string.IsNullOrWhiteSpace(hospital.Id))
        {
            error = "Hospital is required.";
            return false;
        }

        var hospitalNumber = hospital.EffectiveHospitalNumber;
        if (string.IsNullOrWhiteSpace(hospitalNumber))
        {
            error = "Hospital number is required.";
            return false;
        }

        if (cboCardType.SelectedItem is not CardTypeOption type || string.IsNullOrWhiteSpace(type.Id))
        {
            error = "Card type is required.";
            return false;
        }

        if (!UiInputHelper.TryParsePositiveInt(txtBatch.Text, out var batchNumber) || batchNumber <= 0)
        {
            error = "Batch # must be a positive number (CardWritter lô, e.g. 1 → 01).";
            return false;
        }

        try
        {
            cardNumber = CardNumberBuilder.Build(
                hospitalNumber,
                batchNumber,
                serial,
                _settings.Card.BatchNumberWidth,
                _settings.Card.SerialNumberWidth);
            identity = CardNumberBuilder.ToIdentity(cardNumber);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        hospitalId = hospital.Id;
        cardTypeId = type.Id;
        // API batch code: keep numeric lô as zero-padded segment (matches CardWritter group text).
        batchCode = batchNumber.ToString("D" + Math.Max(1, _settings.Card.BatchNumberWidth));
        return true;
    }

    private void StartElapsedTimer()
    {
        StopElapsedTimer();
        _elapsedTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _elapsedTimer.Tick += (_, _) => RefreshBatchCounters();
        _elapsedTimer.Start();
    }

    private void StopElapsedTimer()
    {
        if (_elapsedTimer is null)
            return;
        _elapsedTimer.Stop();
        _elapsedTimer.Dispose();
        _elapsedTimer = null;
    }

    private void RefreshBatchCounters()
    {
        if (!UiInputHelper.TryParsePositiveInt(txtStart.Text, out var start))
            start = 0;
        if (!UiInputHelper.TryParsePositiveInt(txtEnd.Text, out var end))
            end = 0;
        if (!UiInputHelper.TryParsePositiveInt(txtCurrent.Text, out var current))
            current = start;

        var total = Math.Max(0, end - start + 1);
        var remaining = current > end ? 0 : Math.Max(0, end - current + 1);
        var completed = _sessionSuccess;
        var elapsed = _batchElapsed?.Elapsed ?? TimeSpan.Zero;

        batchResult.Update(
            written: _sessionWritten,
            current: current,
            remaining: remaining,
            success: _sessionSuccess,
            failed: _sessionFailed,
            elapsed: elapsed,
            completed: completed,
            total: total,
            start: start,
            end: end);
    }

    private void RefreshConnectionChrome()
    {
        var connected = _connectionService.IsConnected;
        if (connected)
        {
            lblReaderStatus.Text = "Connected";
            lblReaderStatus.ForeColor = UiColors.Success;
            lblStatusBarLeft.Text = "Reader ready";
            lblStatusBarLeft.ForeColor = UiColors.Success;
            lblStatusBarRight.Text = $"Version {DiagnosticsInfo.ApplicationVersion}   ·   Online";
        }
        else
        {
            lblReaderStatus.Text = "Offline";
            lblReaderStatus.ForeColor = UiColors.TextMuted;
            lblStatusBarLeft.Text = "Reader offline";
            lblStatusBarLeft.ForeColor = UiColors.TextMuted;
            lblStatusBarRight.Text = $"Version {DiagnosticsInfo.ApplicationVersion}";
        }

        lblStatusBarRight.Location = new Point(
            statusBar.ClientSize.Width - lblStatusBarRight.Width - 12,
            6);
    }

    private void LogOp(string action, string result)
    {
        operationLog.Append(action, result);
        AppLog.Operation(action, result, null);
    }

    private void RecordTiming(string key, long ms) => _timings[key] = ms + " ms";

    private static void PlaySuccessBeep()
    {
        try
        {
            Console.Beep(880, 120);
        }
        catch
        {
            System.Media.SystemSounds.Asterisk.Play();
        }
    }

    private static void PlayFailBeep()
    {
        try
        {
            Console.Beep(420, 700);
        }
        catch
        {
            System.Media.SystemSounds.Hand.Play();
        }
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
            AppLog.Warn("Reader", $"USB list: {usb.ErrorCode} {usb.Message}");
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

    private bool TryParseRange(out int start, out int end, out int current, out string error)
    {
        start = end = current = 0;
        error = string.Empty;
        if (!UiInputHelper.TryParsePositiveInt(txtStart.Text, out start) ||
            !UiInputHelper.TryParsePositiveInt(txtEnd.Text, out end) ||
            !UiInputHelper.TryParsePositiveInt(txtCurrent.Text, out current))
        {
            error = "Start / End / Current must be numbers.";
            return false;
        }

        if (end < start)
        {
            error = "End must be greater than or equal to Start.";
            return false;
        }

        return true;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        ApplyControlEnablement();
        UseWaitCursor = busy;
    }

    private void SetBatchBusy(bool batchBusy)
    {
        _busy = batchBusy;
        ApplyControlEnablement();
        UseWaitCursor = batchBusy;
    }

    private void ApplyControlEnablement()
    {
        var inputsEnabled = !_busy && !_batchRunning;
        cboHospital.Enabled = inputsEnabled;
        cboCardType.Enabled = inputsEnabled;
        txtBatch.Enabled = inputsEnabled;
        txtStart.Enabled = inputsEnabled;
        txtEnd.Enabled = inputsEnabled;
        cboReader.Enabled = inputsEnabled;
        btnConnect.Enabled = inputsEnabled;
        btnStart.Enabled = inputsEnabled;
        btnSettings.Enabled = inputsEnabled;
        btnStop.Enabled = true;
        txtCurrent.ReadOnly = true;
        txtCurrent.Enabled = true;
    }

    private void SetUiState(UiState state, string detail)
    {
        _uiState = state;
        statusPanel.SetState(state, detail);
        RefreshConnectionChrome();
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
