using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Stages of the CareHR write-card job (matches Application workflow).</summary>
public enum CardWriteJobStage
{
    Idle,
    Scanning,
    Selecting,
    Writing,
    Verifying,
    Registering,
    Completed,
    WrittenButUnregistered,
    Cancelled,
    Failed,
}

/// <summary>Operator snapshot for one write-card job (reader already connected).</summary>
public sealed class CardWriteJobRequest
{
    /// <summary>Initializes a write-card job request.</summary>
    public CardWriteJobRequest(
        CardIdentity intendedIdentity,
        byte[] accessPassword,
        string cardTypeId,
        string batchCode,
        ushort scanTimeoutMs = DeviceConstants.DefaultScanTimeoutMs)
    {
        IntendedIdentity = intendedIdentity ?? throw new ArgumentNullException(nameof(intendedIdentity));
        AccessPassword = accessPassword ?? throw new ArgumentNullException(nameof(accessPassword));
        CardTypeId = cardTypeId ?? string.Empty;
        BatchCode = batchCode ?? string.Empty;
        ScanTimeoutMs = scanTimeoutMs;
    }

    /// <summary>Gets the intended CareHR identity to write.</summary>
    public CardIdentity IntendedIdentity { get; }

    /// <summary>Gets the access password.</summary>
    public byte[] AccessPassword { get; }

    /// <summary>Gets the CareHR card type id for registry.</summary>
    public string CardTypeId { get; }

    /// <summary>Gets the CareHR batch code for registry.</summary>
    public string BatchCode { get; }

    /// <summary>Gets the scan window timeout (ms).</summary>
    public ushort ScanTimeoutMs { get; }
}

/// <summary>End-to-end result of a write-card job.</summary>
public sealed class CardWriteJobResult
{
    /// <summary>Initializes a job result.</summary>
    public CardWriteJobResult(
        bool success,
        CardWriteJobStage stage,
        DeviceErrorCode errorCode,
        string message,
        CardInformation? scannedCard = null,
        CardVerifyResult? verifyResult = null,
        RegistrationResult? registrationResult = null)
    {
        Success = success;
        Stage = stage;
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
        ScannedCard = scannedCard;
        VerifyResult = verifyResult;
        RegistrationResult = registrationResult;
    }

    /// <summary>Gets whether the job completed fully (write + verify + register).</summary>
    public bool Success { get; }

    /// <summary>Gets the stage reached or failed.</summary>
    public CardWriteJobStage Stage { get; }

    /// <summary>Gets the Application error code.</summary>
    public DeviceErrorCode ErrorCode { get; }

    /// <summary>Gets a human-readable message.</summary>
    public string Message { get; }

    /// <summary>Gets the scanned card when available.</summary>
    public CardInformation? ScannedCard { get; }

    /// <summary>Gets the verify result when available.</summary>
    public CardVerifyResult? VerifyResult { get; }

    /// <summary>Gets the registration result when available.</summary>
    public RegistrationResult? RegistrationResult { get; }

    /// <summary>Creates a full success result.</summary>
    public static CardWriteJobResult Complete(
        CardInformation scanned,
        CardVerifyResult verify,
        RegistrationResult registration) =>
        new(
            true,
            CardWriteJobStage.Completed,
            DeviceErrorCode.None,
            "Card written, verified, and registered.",
            scanned,
            verify,
            registration);

    /// <summary>Creates a written-but-unregistered result (API fail after verify).</summary>
    public static CardWriteJobResult WrittenUnregistered(
        CardInformation scanned,
        CardVerifyResult verify,
        RegistrationResult registration) =>
        new(
            false,
            CardWriteJobStage.WrittenButUnregistered,
            DeviceErrorCode.RegistrationFailed,
            registration.Message,
            scanned,
            verify,
            registration);

    /// <summary>Creates a failed job result at a stage.</summary>
    public static CardWriteJobResult Fail(
        CardWriteJobStage stage,
        DeviceErrorCode errorCode,
        string message,
        CardInformation? scanned = null,
        CardVerifyResult? verify = null) =>
        new(false, stage, errorCode, message, scanned, verify);

    /// <summary>Creates a cancelled job result.</summary>
    public static CardWriteJobResult Cancelled(string message = "Operation cancelled.") =>
        new(false, CardWriteJobStage.Cancelled, DeviceErrorCode.None, message);
}
