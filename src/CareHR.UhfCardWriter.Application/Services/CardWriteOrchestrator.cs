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

        // --- Verify (UC-008) — mandatory before register (BR-003, BR-004) ---
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
}
