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

        // Business guard: scanned EPC → logical number → Exists in DB.
        // Checks the card ON THE READER, never the newly generated target.
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

        var write = _writingService.WriteIdentity(
            new CardWriteRequest(request.IntendedIdentity, request.AccessPassword));

        if (!write.Success)
        {
            return CardWriteJobResult.Fail(
                CardWriteJobStage.Writing,
                write.ErrorCode,
                write.Message,
                scanned);
        }

        if (cancellationToken.IsCancellationRequested)
            return CancelAndStop(scanned);

        // Re-select intended EPC before Verify Read (production path).
        _ = _scanningService.SelectCard(request.IntendedIdentity);

        var verify = _verificationService.Verify(
            new CardVerifyRequest(request.IntendedIdentity, request.AccessPassword));

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
}
