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
    public CardWriteJobRequest(
        CardIdentity intendedIdentity,
        byte[] accessPassword,
        string hospitalId,
        string cardTypeId,
        string batchCode,
        ushort scanTimeoutMs = DeviceConstants.DefaultScanTimeoutMs)
    {
        IntendedIdentity = intendedIdentity ?? throw new ArgumentNullException(nameof(intendedIdentity));
        AccessPassword = accessPassword ?? throw new ArgumentNullException(nameof(accessPassword));
        HospitalId = hospitalId ?? string.Empty;
        CardTypeId = cardTypeId ?? string.Empty;
        BatchCode = batchCode ?? string.Empty;
        ScanTimeoutMs = scanTimeoutMs;
    }

    public CardIdentity IntendedIdentity { get; }

    public byte[] AccessPassword { get; }

    public string HospitalId { get; }

    public string CardTypeId { get; }

    public string BatchCode { get; }

    public ushort ScanTimeoutMs { get; }
}

/// <summary>End-to-end result of a write-card job.</summary>
public sealed class CardWriteJobResult
{
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

    public bool Success { get; }

    public CardWriteJobStage Stage { get; }

    public DeviceErrorCode ErrorCode { get; }

    public string Message { get; }

    public CardInformation? ScannedCard { get; }

    public CardVerifyResult? VerifyResult { get; }

    public RegistrationResult? RegistrationResult { get; }

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

    public static CardWriteJobResult Fail(
        CardWriteJobStage stage,
        DeviceErrorCode errorCode,
        string message,
        CardInformation? scanned = null,
        CardVerifyResult? verify = null) =>
        new(false, stage, errorCode, message, scanned, verify);

    public static CardWriteJobResult Cancelled(string message = "Operation cancelled.") =>
        new(false, CardWriteJobStage.Cancelled, DeviceErrorCode.None, message);
}
