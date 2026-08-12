using System.Diagnostics;
using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Diagnostics;
using CareHR.UhfCardWriter.App.Presentation;
using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;
using CareHR.UhfCardWriter.Application.Services;
using CareHR.UhfCardWriter.Infrastructure.Registration;
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
    private readonly CardRegistrationService _registrationService;
    private readonly IWriterAuthSession _authSession;
    private readonly AppSettings _settings;
    private readonly Dictionary<string, string> _timings = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _batchCts;
    private bool _batchRunning;
    private bool _busy;
    private bool _debugMode;
    private UiState _uiState = UiState.Disconnected;
    private bool _resolvingSerial;

    private int _sessionWritten;
    private int _sessionSuccess;
    private int _sessionFailed;
    private Stopwatch? _batchElapsed;
    private System.Windows.Forms.Timer? _elapsedTimer;
    private System.Windows.Forms.Timer? _runAnimTimer;
    private int _runAnimFrame;
    private string? _connectTransientText;

    public MainForm(
        CardConnectionService connectionService,
        CardScanningService scanningService,
        CardWriteOrchestrator orchestrator,
        CardRegistrationService registrationService,
        IWriterAuthSession authSession,
        IOptions<AppSettings> options)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _scanningService = scanningService ?? throw new ArgumentNullException(nameof(scanningService));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
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
            if (_batchRunning || _resolvingSerial)
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
            if (_batchRunning || _resolvingSerial)
                return;
            ResolveNextSerialFromDb();
            RefreshTargetCardPreview();
            RefreshBatchCounters();
        };
        cboHospital.SelectedIndexChanged += (_, _) =>
        {
            if (_batchRunning || _resolvingSerial)
                return;
            ResolveNextSerialFromDb();
            RefreshTargetCardPreview();
            RefreshBatchCounters();
        };
        txtCurrent.TextChanged += (_, _) =>
        {
            if (!_batchRunning)
                RefreshTargetCardPreview();
        };

        RefreshReaders();
        ResolveNextSerialFromDb();
        RefreshTargetCardPreview();
        RefreshBatchCounters();
        ApplyDebugVisibility();
        ApplyConnectionUiState();
        ApplyBatchUiState();
        ApplyControlEnablement();
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
        if (message.Contains("BearerToken", StringComparison.OrdinalIgnoreCase)
            || message.Contains(HttpCardRegistrarAdapter.AuthRequiredMessage, StringComparison.Ordinal))
            return HttpCardRegistrarAdapter.AuthRequiredMessage;
        return UserMessage.SafeMessage(message);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _batchCts?.Cancel();
        StopRunAnimation(restoreStartButton: false);
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
            _connectTransientText = "Disconnecting…";
            ApplyConnectionUiState();
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
                _connectTransientText = null;
                SetUiState(UiState.Disconnected, "Reader disconnected.");
            }
            finally
            {
                _connectTransientText = null;
                SetBusy(false);
                ApplyConnectionUiState();
            }

            return;
        }

        if (!TryBuildEndpoint(out var endpoint, out var error))
        {
            MessageBox.Show(this, error, "Connect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _connectTransientText = "Connecting…";
        ApplyConnectionUiState();
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
                _connectTransientText = null;
                SetUiState(UiState.Failed, msg);
                ApplyConnectionUiState();
                return;
            }

            LogOp("Connect", "Reader connected.");
            LogNativeRuntimeDiagnostics();
            _connectTransientText = null;
            SetUiState(UiState.Ready, "Reader connected. Set range and press Start.");
            ApplyConnectionUiState();
            // One GetDevicePara — never concurrent Out Interface + RF Power loads.
            await LoadDeviceParaAfterConnectAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            connectSw.Stop();
            var msg = UserMessage.ForException(ex);
            AppLog.Error("Presentation", "Connect failed", ex);
            LogOp("Connect", msg);
            _connectTransientText = null;
            SetUiState(UiState.Failed, msg);
            ApplyConnectionUiState();
        }
        finally
        {
            _connectTransientText = null;
            SetBusy(false);
            ApplyConnectionUiState();
        }
    }

    private async void BtnGetOutInterface_Click(object? sender, EventArgs e)
    {
        if (_busy || _batchRunning || !_connectionService.IsConnected)
            return;

        SetBusy(true);
        try
        {
            var threadId = Environment.CurrentManagedThreadId;
            LogOp("DevicePara", $"[DevicePara] GetStart ThreadId={threadId} Op=OutInterface");
            AppLog.Info("DevicePara", $"[DevicePara] GetStart ThreadId={threadId} Op=OutInterface");
            var sw = Stopwatch.StartNew();
            var result = await Task.Run(() => _connectionService.GetOutInterface()).ConfigureAwait(true);
            sw.Stop();
            if (!result.Success)
            {
                LogOp(
                    "Get Out Interface",
                    $"Status=0x{result.VendorStatusCode:X8} Success=false {UserMessage.ForDeviceOrOperation(result.Message)}");
                LogOp("DevicePara", $"[DevicePara] GetElapsedMs={sw.ElapsedMilliseconds}");
                return;
            }

            var raw = result.Value;
            var name = DeviceConstants.FormatOutInterface(raw);
            ApplyOutInterfaceSelection(raw);
            LogOp(
                "Get Out Interface",
                $"Status=0x{result.VendorStatusCode:X8} Success=true Raw={raw} Name={name}");
            LogOp("DevicePara", $"[DevicePara] GetElapsedMs={sw.ElapsedMilliseconds}");
            AppLog.Info("DevicePara", $"InterfaceRaw={raw} Interface={name} Status=0x{result.VendorStatusCode:X8}");
        }
        catch (Exception ex)
        {
            AppLog.Error("Presentation", "Get Out Interface failed", ex);
            LogOp("Get Out Interface", UserMessage.ForException(ex));
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnSetOutInterface_Click(object? sender, EventArgs e)
    {
        if (_busy || _batchRunning || !_connectionService.IsConnected)
            return;

        if (cboOutInterface.SelectedItem is not OutInterfaceListItem selected)
        {
            MessageBox.Show(this, "Select an Out Interface value.", "Out Interface", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        try
        {
            var fromResult = await Task.Run(() => _connectionService.GetOutInterface()).ConfigureAwait(true);
            var fromRaw = fromResult.Success ? fromResult.Value : (byte)0;
            var fromName = DeviceConstants.FormatOutInterface(fromRaw);
            var toRaw = selected.Raw;
            var toName = selected.Name;

            LogOp("DevicePara", $"[DevicePara] SetStart ThreadId={Environment.CurrentManagedThreadId} Op=OutInterface");
            var set = await Task.Run(() => _connectionService.SetOutInterface(toRaw)).ConfigureAwait(true);
            if (!set.Success)
            {
                LogOp(
                    "Set Out Interface",
                    $"Status=0x{set.VendorStatusCode:X8} Success=false {UserMessage.ForDeviceOrOperation(set.Message)}");
                return;
            }

            var verified = set.Value;
            var verifiedName = DeviceConstants.FormatOutInterface(verified);
            ApplyOutInterfaceSelection(verified);
            LogOp(
                "Set Out Interface",
                $"Status=0x{set.VendorStatusCode:X8} From={fromRaw} {fromName} To={toRaw} {toName} SetDevicePara=OK Verify={verified} {verifiedName}");
            LogOp("DevicePara", $"[DevicePara] SetResult Status=0x{set.VendorStatusCode:X8} Success=true");
            LogOp("DevicePara", $"[DevicePara] VerifyResult Status=0x{set.VendorStatusCode:X8}");
            AppLog.Info("DevicePara", $"InterfaceRaw={verified} Interface={verifiedName}");
        }
        catch (Exception ex)
        {
            AppLog.Error("Presentation", "Set Out Interface failed", ex);
            LogOp("Set Out Interface", UserMessage.ForException(ex));
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Post-connect: exactly one GetDevicePara → Out Interface + RF Power.
    /// </summary>
    private async Task LoadDeviceParaAfterConnectAsync()
    {
        try
        {
            var threadId = Environment.CurrentManagedThreadId;
            LogOp("DevicePara", $"[DevicePara] GetStart ThreadId={threadId} Op=PostConnect");
            AppLog.Info("DevicePara", $"[DevicePara] GetStart ThreadId={threadId} Op=PostConnect");
            var sw = Stopwatch.StartNew();
            var result = await Task.Run(() => _connectionService.GetDeviceParameters()).ConfigureAwait(true);
            sw.Stop();

            if (!result.Success || result.Value is null)
            {
                var msg = UserMessage.ForDeviceOrOperation(result.Message);
                LogOp(
                    "DevicePara",
                    $"[DevicePara] GetResult Status=0x{result.VendorStatusCode:X8} Success=false ThreadId={threadId}");
                LogOp("DevicePara", $"[DevicePara] GetElapsedMs={sw.ElapsedMilliseconds}");
                LogOp("Get Out Interface", msg);
                LogOp("RFPower", $"[RFPower] GetResult Status=0x{result.VendorStatusCode:X8} Success=false {msg}");
                return;
            }

            var para = result.Value;
            var ifaceName = DeviceConstants.FormatOutInterface(para.Interface);
            ApplyOutInterfaceSelection(para.Interface);
            LogOp(
                "Get Out Interface",
                $"Status=0x{result.VendorStatusCode:X8} Success=true Raw={para.Interface} Name={ifaceName}");
            AppLog.Info("DevicePara", $"InterfaceRaw={para.Interface} Interface={ifaceName}");

            ApplyRfPowerSelection(para.RfidPower);
            LogOp(
                "RFPower",
                $"[RFPower] GetResult Status=0x{result.VendorStatusCode:X8} Success=true Actual={para.RfidPower}");
            LogOp(
                "DevicePara",
                $"[DevicePara] GetResult Status=0x{result.VendorStatusCode:X8} Success=true ThreadId={threadId}");
            LogOp("DevicePara", $"[DevicePara] GetElapsedMs={sw.ElapsedMilliseconds}");
            AppLog.Info(
                "DevicePara",
                $"[DevicePara] GetResult Status=0x{result.VendorStatusCode:X8} Success=true " +
                $"Interface={para.Interface} RfidPower={para.RfidPower} ElapsedMs={sw.ElapsedMilliseconds}");
        }
        catch (Exception ex)
        {
            AppLog.Error("Presentation", "Load DevicePara after connect failed", ex);
            LogOp("DevicePara", UserMessage.ForException(ex));
        }
    }

    private void ApplyOutInterfaceSelection(byte raw)
    {
        var items = cboOutInterface.Items.Cast<OutInterfaceListItem>().ToList();
        var match = items.Find(i => i.Raw == raw);
        if (match is not null)
        {
            cboOutInterface.SelectedItem = match;
            return;
        }

        // Unknown vendor value: keep combo unchanged but log name via FormatOutInterface.
    }

    private async void BtnGetRfPower_Click(object? sender, EventArgs e)
    {
        if (_busy || _batchRunning || !_connectionService.IsConnected)
            return;

        SetBusy(true);
        try
        {
            var threadId = Environment.CurrentManagedThreadId;
            LogOp("RFPower", "[RFPower] GetStart");
            LogOp("DevicePara", $"[DevicePara] GetStart ThreadId={threadId} Op=RfPower");
            AppLog.Info("RFPower", "[RFPower] GetStart");
            var sw = Stopwatch.StartNew();
            var result = await Task.Run(() => _connectionService.GetRfPower()).ConfigureAwait(true);
            sw.Stop();
            if (!result.Success)
            {
                var msg = UserMessage.ForDeviceOrOperation(result.Message);
                LogOp(
                    "RFPower",
                    $"[RFPower] GetResult Status=0x{result.VendorStatusCode:X8} Success=false {msg}");
                LogOp("DevicePara", $"[DevicePara] GetElapsedMs={sw.ElapsedMilliseconds}");
                AppLog.Warn(
                    "RFPower",
                    $"[RFPower] GetResult Status=0x{result.VendorStatusCode:X8} Success=false Message={result.Message}");
                return;
            }

            ApplyRfPowerSelection(result.Value);
            LogOp(
                "RFPower",
                $"[RFPower] GetResult Status=0x{result.VendorStatusCode:X8} Success=true Actual={result.Value}");
            LogOp("DevicePara", $"[DevicePara] GetElapsedMs={sw.ElapsedMilliseconds}");
            AppLog.Info(
                "RFPower",
                $"[RFPower] GetResult Status=0x{result.VendorStatusCode:X8} Success=true Actual={result.Value}");
        }
        catch (Exception ex)
        {
            AppLog.Error("Presentation", "Get RF Power failed", ex);
            LogOp("RFPower", UserMessage.ForException(ex));
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnSetRfPower_Click(object? sender, EventArgs e)
    {
        if (_busy || _batchRunning || !_connectionService.IsConnected)
            return;

        if (!TryGetSelectedRfPowerDbm(out var requested, out var parseFailReason))
        {
            MessageBox.Show(
                this,
                string.IsNullOrWhiteSpace(parseFailReason)
                    ? $"Select RF Power ({DeviceConstants.RfPowerMinDbm}–{DeviceConstants.RfPowerMaxDbm} dBm)."
                    : parseFailReason,
                "RF Power",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        try
        {
            LogOp("RFPower", $"[RFPower] SetStart Requested={requested}");
            AppLog.Info("RFPower", $"[RFPower] SetStart Requested={requested}");

            var set = await Task.Run(() => _connectionService.SetRfPower(requested)).ConfigureAwait(true);
            if (!set.Success)
            {
                var msg = UserMessage.ForDeviceOrOperation(set.Message);
                LogOp(
                    "RFPower",
                    $"[RFPower] SetResult Status=0x{set.VendorStatusCode:X8} Success=false {msg}");
                AppLog.Warn(
                    "RFPower",
                    $"[RFPower] SetResult Status=0x{set.VendorStatusCode:X8} Success=false Message={set.Message}");
                MessageBox.Show(
                    this,
                    "Không thể thiết lập RF Power. " + msg,
                    "RF Power",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                var fallback = await Task.Run(() => _connectionService.GetRfPower()).ConfigureAwait(true);
                if (fallback.Success)
                    ApplyRfPowerSelection(fallback.Value);
                return;
            }

            ApplyRfPowerSelection(set.Value);
            LogOp(
                "RFPower",
                $"[RFPower] SetResult Status=0x{set.VendorStatusCode:X8} Success=true Requested={requested} Actual={set.Value}");
            AppLog.Info(
                "RFPower",
                $"[RFPower] VerifyResult Status=0x{set.VendorStatusCode:X8} Actual={set.Value}");
        }
        catch (Exception ex)
        {
            AppLog.Error("Presentation", "Set RF Power failed", ex);
            LogOp("RFPower", UserMessage.ForException(ex));
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Reads combo selection as dBm. Items are stored as <see cref="int"/> 0..33.
    /// Accepts boxed int/byte for resilience; rejects null / non-numeric / out of range.
    /// </summary>
    private bool TryGetSelectedRfPowerDbm(out byte powerDbm, out string failReason)
    {
        powerDbm = 0;
        failReason = string.Empty;

        var selected = cboRfPower.SelectedItem;
        if (selected is null || cboRfPower.SelectedIndex < 0)
        {
            failReason = $"Select RF Power ({DeviceConstants.RfPowerMinDbm}–{DeviceConstants.RfPowerMaxDbm} dBm).";
            return false;
        }

        int value;
        switch (selected)
        {
            case int i:
                value = i;
                break;
            case byte b:
                value = b;
                break;
            case short s:
                value = s;
                break;
            case long l when l is >= int.MinValue and <= int.MaxValue:
                value = (int)l;
                break;
            default:
                if (!int.TryParse(
                        Convert.ToString(selected, System.Globalization.CultureInfo.InvariantCulture),
                        System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out value))
                {
                    failReason = $"Select RF Power ({DeviceConstants.RfPowerMinDbm}–{DeviceConstants.RfPowerMaxDbm} dBm).";
                    return false;
                }

                break;
        }

        if (value < DeviceConstants.RfPowerMinDbm || value > DeviceConstants.RfPowerMaxDbm)
        {
            failReason =
                $"RF Power must be {DeviceConstants.RfPowerMinDbm}–{DeviceConstants.RfPowerMaxDbm} dBm.";
            return false;
        }

        powerDbm = (byte)value;
        return true;
    }

    private void ApplyRfPowerSelection(byte powerDbm)
    {
        var asInt = (int)powerDbm;
        // Items are int — Contains must use int (byte Equals fails against boxed int).
        if (!cboRfPower.Items.Contains(asInt))
            cboRfPower.Items.Add(asInt);
        cboRfPower.SelectedItem = asInt;
    }

    private static void LogNativeRuntimeDiagnostics()
    {
        var uhf = DiagnosticsInfo.DescribeNativeDll("UHFPrimeReader.dll");
        var hid = DiagnosticsInfo.DescribeNativeDll("hidapi.dll");
        AppLog.Info(
            "Connect",
            $"Architecture={DiagnosticsInfo.ProcessArchitecture} " +
            $"UHFPrimeReader={uhf.Path} Version={uhf.Version} Size={uhf.Size} Arch={uhf.Arch} SHA256={uhf.Sha256} " +
            $"hidapi={hid.Path} Size={hid.Size} Arch={hid.Arch}");
        AppLog.Info("Native", $"Loaded UHFPrimeReader: {uhf.Path}");
        AppLog.Info("Native", $"Loaded hidapi: {hid.Path}");
        AppLog.Info("Native", $"Architecture: {DiagnosticsInfo.ProcessArchitecture}");
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

        // Refresh next serial from DB before starting (HospitalNumber + Batch scope).
        ResolveNextSerialFromDb();

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
        StartRunAnimation();
        SetBatchBusy(true);
        SetUiState(UiState.Busy, "Starting batch…");

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
                SetUiState(UiState.WaitingForCard, $"Đang chờ quét thẻ… ({cardDisplay})");
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
                    SetUiState(UiState.Ready, "STOPPED — Batch stopped.");
                    break;
                }

                if (_batchCts.IsCancellationRequested || scan.Outcome == ScanOutcome.Cancelled)
                {
                    LogOp("Stop", "Batch stopped.");
                    SetUiState(UiState.Ready, "STOPPED — Batch stopped.");
                    break;
                }

                if (!scan.Success || scan.Card is null)
                {
                    SetUiState(UiState.WaitingForCard, $"Đang chờ quét thẻ… ({cardDisplay})");
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

                SetUiState(UiState.Writing, $"Đang ghi thẻ… {cardDisplay}");
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

                // Business guard: existing card or Exists API fail-closed — warn, skip write, keep batch + Current.
                if (result.Stage is CardWriteJobStage.SkippedAlreadyRegistered
                    or CardWriteJobStage.SkippedExistsCheckFailed)
                {
                    var existing = CardNumberBuilder.ToCardNumberFromEpcBytes(scan.Card.Identity.Epc);
                    var existingDisplay = string.IsNullOrWhiteSpace(existing)
                        ? scan.Card.Identity.EpcHex
                        : FormatCardDisplay(existing);
                    var warn = string.IsNullOrWhiteSpace(result.Message)
                        ? (result.Stage == CardWriteJobStage.SkippedAlreadyRegistered
                            ? $"Thẻ RFID {existingDisplay} đã được đăng ký."
                            : $"Không thể kiểm tra thẻ RFID {existingDisplay}. Không thực hiện ghi.")
                        : result.Message;
                    LogOp("Guard", warn);
                    SetUiState(UiState.WaitingForCard, warn);
                    PlayFailBeep();
                    RefreshBatchCounters();
                    try
                    {
                        await Task.Delay(800, _batchCts.Token).ConfigureAwait(true);
                    }
                    catch (OperationCanceledException)
                    {
                        LogOp("Stop", "Batch stopped.");
                        SetUiState(UiState.Ready, "Batch stopped.");
                        break;
                    }

                    continue;
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
            StopRunAnimation(restoreStartButton: true);
            SetBatchBusy(false);
            if (_connectionService.IsConnected &&
                _uiState is not UiState.Failed and not UiState.Done)
            {
                SetUiState(UiState.Ready, "Stopped. Ready for next card. Press Start to continue.");
            }
            else
            {
                ApplyBatchUiState();
                ApplyConnectionUiState();
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
            SetUiState(UiState.Busy, "STOPPING…");
            btnStart.Text = "STOPPING…";
            btnStart.BackColor = UiColors.CareHrBlue;
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
            _authSession,
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
        // Kept for compatibility; visual state is owned by ApplyConnectionUiState.
        _connectTransientText = null;
        ApplyConnectionUiState();
        _ = connected;
    }

    private void ApplyConnectionUiState()
    {
        var connected = _connectionService.IsConnected;

        if (!string.IsNullOrWhiteSpace(_connectTransientText))
        {
            btnConnect.Text = _connectTransientText;
            btnConnect.BackColor = UiColors.Warning;
            btnConnect.ForeColor = Color.White;
            lblReaderStatus.Text = connected ? "● …" : "○ …";
            lblReaderStatus.ForeColor = UiColors.Warning;
            lblStatusBarLeft.Text = _connectTransientText;
            lblStatusBarLeft.ForeColor = UiColors.Warning;
        }
        else if (connected)
        {
            btnConnect.Text = "Disconnect";
            btnConnect.BackColor = UiColors.CareHrBlue;
            btnConnect.ForeColor = Color.White;
            lblReaderStatus.Text = "● Online";
            lblReaderStatus.ForeColor = UiColors.Success;
            lblStatusBarLeft.Text = "●  Reader connected";
            lblStatusBarLeft.ForeColor = UiColors.Success;
            lblStatusBarRight.Text = $"Version {DiagnosticsInfo.ApplicationVersion}   ·   Online";
        }
        else
        {
            btnConnect.Text = "Connect";
            btnConnect.BackColor = UiColors.CareHrBlue;
            btnConnect.ForeColor = Color.White;
            lblReaderStatus.Text = "○ Offline";
            lblReaderStatus.ForeColor = UiColors.TextMuted;
            lblStatusBarLeft.Text = "Reader disconnected";
            lblStatusBarLeft.ForeColor = UiColors.TextMuted;
            lblStatusBarRight.Text = $"Version {DiagnosticsInfo.ApplicationVersion}";
        }

        lblStatusBarRight.Location = new Point(
            statusBar.ClientSize.Width - lblStatusBarRight.Width - 12,
            6);
    }

    private void ApplyBatchUiState()
    {
        if (_batchRunning)
        {
            btnStop.BackColor = Color.FromArgb(170, 55, 55);
            btnStop.ForeColor = Color.White;
            if (_runAnimTimer is null)
                btnStart.Text = "RUNNING";
            btnStart.BackColor = UiColors.CareHrBlue;
            btnStart.ForeColor = Color.White;
        }
        else
        {
            btnStart.Text = "Start";
            btnStart.BackColor = UiColors.CareHrBlue;
            btnStart.ForeColor = Color.White;
            btnStop.BackColor = Color.FromArgb(158, 158, 158);
            btnStop.ForeColor = Color.White;
        }
    }

    private void StartRunAnimation()
    {
        StopRunAnimation(restoreStartButton: false);
        _runAnimFrame = 0;
        btnStart.BackColor = UiColors.CareHrBlue;
        btnStart.ForeColor = Color.White;
        btnStart.Text = "RUNNING";
        _runAnimTimer = new System.Windows.Forms.Timer { Interval = 280 };
        _runAnimTimer.Tick += RunAnimTimer_Tick;
        _runAnimTimer.Start();
    }

    private void RunAnimTimer_Tick(object? sender, EventArgs e)
    {
        if (!_batchRunning)
        {
            StopRunAnimation(restoreStartButton: true);
            return;
        }

        if (string.Equals(btnStart.Text, "STOPPING…", StringComparison.Ordinal))
            return;

        // Soft text-only cue on Start (no color flash) — primary animation is StatusPanel.
        var dots = _runAnimFrame % 4;
        btnStart.Text = dots == 0 ? "RUNNING" : "RUNNING" + new string('.', dots);
        _runAnimFrame++;
    }

    private void StopRunAnimation(bool restoreStartButton)
    {
        if (_runAnimTimer is not null)
        {
            _runAnimTimer.Stop();
            _runAnimTimer.Tick -= RunAnimTimer_Tick;
            _runAnimTimer.Dispose();
            _runAnimTimer = null;
        }

        _runAnimFrame = 0;
        if (restoreStartButton && !_batchRunning)
        {
            btnStart.Text = "Start";
            btnStart.BackColor = UiColors.CareHrBlue;
            btnStart.ForeColor = Color.White;
        }
    }

    private void SyncCurrentFromStart()
    {
        if (UiInputHelper.TryParsePositiveInt(txtStart.Text, out var start))
            txtCurrent.Text = start.ToString();
    }

    /// <summary>
    /// Sets Current (and Start) to MAX(serial for HospitalNumber+Batch) + 1 from CareHR API.
    /// Does not reset to 1 when DB already has numbers for the prefix.
    /// </summary>
    private void ResolveNextSerialFromDb()
    {
        if (_batchRunning)
            return;

        if (cboHospital.SelectedItem is not HospitalOption hospital ||
            string.IsNullOrWhiteSpace(hospital.Id))
        {
            return;
        }

        var hospitalNumber = hospital.EffectiveHospitalNumber;
        if (string.IsNullOrWhiteSpace(hospitalNumber))
            return;

        if (!UiInputHelper.TryParsePositiveInt(txtBatch.Text, out var batchNumber) || batchNumber <= 0)
            return;

        var batchWidth = Math.Max(1, _settings.Card.BatchNumberWidth);
        var serialWidth = Math.Max(1, _settings.Card.SerialNumberWidth);
        var batchPart = batchNumber.ToString("D" + batchWidth, System.Globalization.CultureInfo.InvariantCulture);
        var prefix = hospitalNumber.Trim() + batchPart;

        NextSerialResult next;
        try
        {
            next = _registrationService.GetNextSerial(hospital.Id, prefix, serialWidth);
        }
        catch (Exception ex)
        {
            LogOp("Sequence", "Không lấy được serial tiếp theo: " + UserMessage.ForException(ex));
            return;
        }

        if (!next.Success)
        {
            LogOp("Sequence", "Không lấy được serial tiếp theo: " + next.Message);
            return;
        }

        _resolvingSerial = true;
        try
        {
            txtCurrent.Text = next.NextSerial.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // Keep Start aligned with next so range begins at the resolved serial.
            txtStart.Text = txtCurrent.Text;
            if (UiInputHelper.TryParsePositiveInt(txtEnd.Text, out var end) && end < next.NextSerial)
                txtEnd.Text = Math.Max(next.NextSerial, next.NextSerial + 99).ToString(System.Globalization.CultureInfo.InvariantCulture);

            LogOp("Sequence", $"Next serial for {prefix} = {next.NextSerial}.");
        }
        finally
        {
            _resolvingSerial = false;
        }
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

    private void RefreshConnectionChrome() => ApplyConnectionUiState();

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
        var connected = _connectionService.IsConnected;
        var inputsEnabled = !_busy && !_batchRunning;
        cboHospital.Enabled = inputsEnabled;
        cboCardType.Enabled = inputsEnabled;
        txtBatch.Enabled = inputsEnabled;
        txtStart.Enabled = inputsEnabled;
        txtEnd.Enabled = inputsEnabled;
        cboReader.Enabled = inputsEnabled && !connected;
        btnConnect.Enabled = !_batchRunning && !_busy;
        btnStart.Enabled = inputsEnabled && connected;
        btnSettings.Enabled = inputsEnabled;
        btnStop.Enabled = _batchRunning;
        var deviceControlsEnabled = inputsEnabled && connected;
        cboOutInterface.Enabled = deviceControlsEnabled;
        btnGetOutInterface.Enabled = deviceControlsEnabled;
        btnSetOutInterface.Enabled = deviceControlsEnabled;
        cboRfPower.Enabled = deviceControlsEnabled;
        btnGetRfPower.Enabled = deviceControlsEnabled;
        btnSetRfPower.Enabled = deviceControlsEnabled;
        txtCurrent.ReadOnly = true;
        txtCurrent.Enabled = true;
        ApplyBatchUiState();
        ApplyConnectionUiState();
    }

    private void SetUiState(UiState state, string detail)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetUiState(state, detail));
            return;
        }

        _uiState = state;
        statusPanel.SetState(state, detail);
        ApplyConnectionUiState();
        ApplyBatchUiState();
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

    internal sealed class OutInterfaceListItem
    {
        public OutInterfaceListItem(string name, byte raw)
        {
            Name = name;
            Raw = raw;
        }

        public string Name { get; }
        public byte Raw { get; }
        public override string ToString() => Name;
    }
}
