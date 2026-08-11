using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Orchestrates the CareHR write-card job (UC-004 → UC-009, UC-010).
/// </summary>
/// <remarks>
/// Connect is a precondition (reader already connected). No SDK calls — only Application services.
/// </remarks>
public sealed class CardWriteOrchestrator
{
    private readonly CardConnectionService _connectionService;
    private readonly CardScanningService _scanningService;
    private readonly CardWritingService _writingService;
    private readonly CardVerificationService _verificationService;
    private readonly CardRegistrationService _registrationService;

    public CardWriteOrchestrator(
        CardConnectionService connectionService,
        CardScanningService scanningService,
        CardWritingService writingService,
        CardVerificationService verificationService,
        CardRegistrationService registrationService)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _scanningService = scanningService ?? throw new ArgumentNullException(nameof(scanningService));
        _writingService = writingService ?? throw new ArgumentNullException(nameof(writingService));
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
    }

    /// <summary>
    /// Runs the MVP write workflow: Scan → Select → Write → Verify → Register.
    /// When <paramref name="alreadyScanned"/> is provided, Scan is skipped (Presentation showed Factory EPC first).
    /// </summary>
    public CardWriteJobResult RunWriteCardJob(
        CardWriteJobRequest request,
        CancellationToken cancellationToken = default,
        CardInformation? alreadyScanned = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        CardValidation.EnsureIdentity(request.IntendedIdentity);
        CardValidation.EnsureAccessPassword(request.AccessPassword);

        if (!_connectionService.IsConnected)
        {
            return CardWriteJobResult.Fail(
                CardWriteJobStage.Failed,
                DeviceErrorCode.ReaderNotConnected,
                "Reader must be connected before write job (BR-001).");
        }

        if (string.IsNullOrWhiteSpace(request.HospitalId) ||
            string.IsNullOrWhiteSpace(request.CardTypeId) ||
            string.IsNullOrWhiteSpace(request.BatchCode))
        {
            return CardWriteJobResult.Fail(
                CardWriteJobStage.Failed,
                DeviceErrorCode.InvalidParameter,
                "Hospital id, card type id and batch code are required.");
        }

        CardInformation scanned;
        if (alreadyScanned is not null)
        {
            scanned = alreadyScanned;
        }
        else
        {
            // --- Scan (UC-004) ---
            if (cancellationToken.IsCancellationRequested)
                return CancelAndStop();

            ScanResult scan;
            try
            {
                scan = _scanningService.ScanForSingleCard(request.ScanTimeoutMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return CancelAndStop();
            }

            if (cancellationToken.IsCancellationRequested)
                return CancelAndStop();

            if (scan.Outcome == ScanOutcome.Cancelled)
                return CancelAndStop();

            if (!scan.Success || scan.Card is null)
            {
                return CardWriteJobResult.Fail(
                    CardWriteJobStage.Scanning,
                    scan.ErrorCode,
                    scan.Message);
            }

            scanned = scan.Card;
        }

        // --- Business guard: scanned EPC → logical number → Exists in DB ---
        // Checks the card ON THE READER (e.g. 790480100006), never the newly generated target.
        // Exists API error → fail-closed (no Write).
        var scannedNumber = CardNumberBuilder.ToCardNumberFromEpcBytes(scanned.Identity.Epc);
        if (LooksLikeCareHrCardNumber(scannedNumber))
        {
            var existence = _registrationService.Exists(request.HospitalId, scannedNumber);
            if (!existence.QuerySucceeded)
            {
                return CardWriteJobResult.SkippedExistsCheckFailed(
                    scanned,
                    scannedNumber,
                    $"Không thể kiểm tra thẻ RFID {scannedNumber}. Không thực hiện ghi để tránh ghi đè dữ liệu.");
            }

            if (existence.Exists)
            {
                return CardWriteJobResult.SkippedAlreadyRegistered(
                    scanned,
                    scannedNumber,
                    $"Thẻ RFID {scannedNumber} đã được đăng ký.");
            }
        }

        // --- Select (UC-005) ---
        var select = _scanningService.SelectCard(scanned.Identity);
        if (!select.Success)
        {
            return CardWriteJobResult.Fail(
                CardWriteJobStage.Selecting,
                select.ErrorCode,
                select.Message,
                scanned);
        }

        if (cancellationToken.IsCancellationRequested)
            return CancelAndStop(scanned);

        // --- Write (UC-007) ---
        var write = _writingService.WriteIdentity(
            new CardWriteRequest(request.IntendedIdentity, request.AccessPassword));

        // Controlled R8 experiment: when CAREHR_UHF_WRITE_TEST=Sleep20, inventory after write
        // (pass or fail) and log under [TEST-SLEEP20]. Does not change write algorithm.
        if (IsSleep20DiagEnabled())
            LogSleep20InventoryAfterWrite(scanned, request.IntendedIdentity, write, request.ScanTimeoutMs);

        if (!write.Success)
        {
            // T2 diagnostic only: Inventory immediately after Write-response fail.
            // Does not change Write / Select / password / bank / payload behavior.
            if (!IsSleep20DiagEnabled())
                LogT2WriteFailureAndInventory(scanned, request.IntendedIdentity, write, request.ScanTimeoutMs);
            return CardWriteJobResult.Fail(
                CardWriteJobStage.Writing,
                write.ErrorCode,
                write.Message,
                scanned);
        }

        if (cancellationToken.IsCancellationRequested)
            return CancelAndStop(scanned);

        // --- VerifyTest (diagnostic only): one variable = Re-Select NEW EPC before Verify Read ---
        // Does not change WriteTag / GetTagResp / password / bank / wordPtr / payload / Inventory.
        WriteT2Diag($"[VerifyTest] FactoryEpc={scanned.Identity.EpcHex}");
        WriteT2Diag($"[VerifyTest] IntendedEpcHex={request.IntendedIdentity.EpcHex}");
        WriteT2Diag($"[VerifyTest] ReSelectStart Epc={request.IntendedIdentity.EpcHex}");
        var reSelect = _scanningService.SelectCard(request.IntendedIdentity);
        WriteT2Diag(
            $"[VerifyTest] ReSelectResult Success={reSelect.Success} " +
            $"ErrorCode={reSelect.ErrorCode} Message={reSelect.Message}");
        if (!reSelect.Success)
            WriteT2Diag("[VerifyTest] FailAt=ReSelect");

        // --- Verify (UC-008) — mandatory before register (BR-003, BR-004) ---
        // Read path unchanged; Select mask is the only variable under test.
        var verify = _verificationService.Verify(
            new CardVerifyRequest(request.IntendedIdentity, request.AccessPassword));

        WriteT2Diag(
            $"[VerifyTest] ActualEpcHex={(verify.ActualIdentity is null ? string.Empty : verify.ActualIdentity.EpcHex)}");
        if (verify.Success)
        {
            WriteT2Diag("[VerifyTest] Result=PASS");
            WriteT2Diag("[VerifyTest] FailAt=NONE");
        }
        else
        {
            WriteT2Diag($"[VerifyTest] Result=FAIL ErrorCode={verify.ErrorCode} Message={verify.Message}");
            if (reSelect.Success)
                WriteT2Diag("[VerifyTest] FailAt=VerifyReadOrCompare");
        }

        if (!verify.Success)
        {
            return CardWriteJobResult.Fail(
                CardWriteJobStage.Verifying,
                verify.ErrorCode,
                verify.Message,
                scanned,
                verify);
        }

        if (cancellationToken.IsCancellationRequested)
            return CancelAndStop(scanned);

        // --- Register (UC-009) ---
        var registration = _registrationService.Register(
            new RegistrationRequest(
                request.IntendedIdentity,
                request.HospitalId,
                request.CardTypeId,
                request.BatchCode,
                isVerified: true));

        if (!registration.Success)
        {
            // Physical card already written — do not rewrite; Operator reconciles (workflow).
            return CardWriteJobResult.WrittenUnregistered(scanned, verify, registration);
        }

        return CardWriteJobResult.Complete(scanned, verify, registration);
    }

    /// <summary>Cancels in-progress scan/write path (UC-010) — stop inventory best-effort.</summary>
    public CardWriteJobResult CancelOperation()
    {
        _ = _scanningService.StopScan();
        return CardWriteJobResult.Cancelled();
    }

    private CardWriteJobResult CancelAndStop(CardInformation? scanned = null)
    {
        _ = _scanningService.StopScan();
        return scanned is null
            ? CardWriteJobResult.Cancelled()
            : new CardWriteJobResult(
                false,
                CardWriteJobStage.Cancelled,
                DeviceErrorCode.None,
                "Operation cancelled.",
                scanned);
    }

    /// <summary>
    /// T2: after Write fails (typically GetTagResp), Inventory once without rewriting.
    /// Logs to the same write-diag.log used by Sdk Write diagnostics.
    /// </summary>
    private void LogT2WriteFailureAndInventory(
        CardInformation scanned,
        CardIdentity intended,
        DeviceResult<CardWriteResult> write,
        ushort scanTimeoutMs)
    {
        var oldEpc = scanned.Identity.EpcHex;
        var oldLen = scanned.Identity.Epc.Length;
        var intendedHex = Convert.ToHexString(intended.Epc);
        WriteT2Diag($"[T2] OldEpc={oldEpc} OldEpcLength={oldLen}");
        WriteT2Diag($"[T2] IntendedEpcHex={intendedHex} IntendedLength={intended.Epc.Length}");
        WriteT2Diag($"[T2] WriteResult Success={write.Success} ErrorCode={write.ErrorCode} Message={write.Message}");

        try
        {
            var after = _scanningService.ScanForSingleCard(scanTimeoutMs);
            if (after.Success && after.Card is not null)
            {
                var epc = after.Card.Identity.EpcHex;
                var len = after.Card.Identity.Epc.Length;
                WriteT2Diag($"[T2] InventoryAfterWrite=OK Outcome={after.Outcome}");
                WriteT2Diag($"[T2] InventoryAfterWriteEpc={epc}");
                WriteT2Diag($"[T2] InventoryAfterWriteCodeLength={len}");
            }
            else
            {
                WriteT2Diag(
                    $"[T2] InventoryAfterWrite=FAIL Outcome={after.Outcome} " +
                    $"ErrorCode={after.ErrorCode} Message={after.Message}");
                WriteT2Diag("[T2] InventoryAfterWriteEpc=");
                WriteT2Diag("[T2] InventoryAfterWriteCodeLength=0");
            }
        }
        catch (Exception ex)
        {
            WriteT2Diag($"[T2] InventoryAfterWrite=EXCEPTION {ex.GetType().Name}: {ex.Message}");
            WriteT2Diag("[T2] InventoryAfterWriteEpc=");
            WriteT2Diag("[T2] InventoryAfterWriteCodeLength=0");
        }
    }

    /// <summary>
    /// R8 controlled experiment inventory after Write. Env: <c>CAREHR_UHF_WRITE_TEST=Sleep20</c>.
    /// </summary>
    private void LogSleep20InventoryAfterWrite(
        CardInformation scanned,
        CardIdentity intended,
        DeviceResult<CardWriteResult> write,
        ushort scanTimeoutMs)
    {
        WriteT2Diag($"[TEST-SLEEP20] WriteResult Success={write.Success} ErrorCode={write.ErrorCode} Message={write.Message}");
        WriteT2Diag($"[TEST-SLEEP20] OldEpc={scanned.Identity.EpcHex}");
        WriteT2Diag($"[TEST-SLEEP20] IntendedEpcHex={Convert.ToHexString(intended.Epc)}");

        try
        {
            var after = _scanningService.ScanForSingleCard(scanTimeoutMs);
            if (after.Success && after.Card is not null)
            {
                WriteT2Diag($"[TEST-SLEEP20] InventoryAfterWrite=OK Outcome={after.Outcome}");
                WriteT2Diag($"[TEST-SLEEP20] EPC={after.Card.Identity.EpcHex}");
                WriteT2Diag($"[TEST-SLEEP20] EPCLength={after.Card.Identity.Epc.Length}");
            }
            else
            {
                WriteT2Diag(
                    $"[TEST-SLEEP20] InventoryAfterWrite=FAIL Outcome={after.Outcome} " +
                    $"ErrorCode={after.ErrorCode} Message={after.Message}");
                WriteT2Diag("[TEST-SLEEP20] EPC=");
                WriteT2Diag("[TEST-SLEEP20] EPCLength=0");
            }
        }
        catch (Exception ex)
        {
            WriteT2Diag($"[TEST-SLEEP20] InventoryAfterWrite=EXCEPTION {ex.GetType().Name}: {ex.Message}");
            WriteT2Diag("[TEST-SLEEP20] EPC=");
            WriteT2Diag("[TEST-SLEEP20] EPCLength=0");
        }
    }

    private static bool IsSleep20DiagEnabled()
    {
        var mode = Environment.GetEnvironmentVariable("CAREHR_UHF_WRITE_TEST");
        return string.Equals(mode, "Sleep20", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// CareHR logical numbers are decimal digits (Hospital+Batch+Serial).
    /// Factory chip EPCs decode to hex and are skipped by the existence guard.
    /// </summary>
    private static bool LooksLikeCareHrCardNumber(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return false;

        var s = cardNumber.Trim();
        if (s.Length < 8)
            return false;

        for (var i = 0; i < s.Length; i++)
        {
            if (!char.IsDigit(s[i]))
                return false;
        }

        return true;
    }

    private static void WriteT2Diag(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CareHR",
                "UhfCardWriter",
                "logs");
            Directory.CreateDirectory(dir);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{message}";
            File.AppendAllText(Path.Combine(dir, "write-diag.log"), line + Environment.NewLine);
        }
        catch
        {
            // Diagnostic only.
        }
    }
}
