using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Result of UC-008 Verify Card.</summary>
public sealed class CardVerifyResult
{
    public CardVerifyResult(
        bool isMatch,
        CardIdentity intendedIdentity,
        CardIdentity? actualIdentity,
        DeviceErrorCode errorCode,
        string message)
    {
        IsMatch = isMatch;
        IntendedIdentity = intendedIdentity ?? throw new ArgumentNullException(nameof(intendedIdentity));
        ActualIdentity = actualIdentity;
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
    }

    public bool IsMatch { get; }

    public CardIdentity IntendedIdentity { get; }

    public CardIdentity? ActualIdentity { get; }

    public DeviceErrorCode ErrorCode { get; }

    public string Message { get; }

    public bool Success => IsMatch && ErrorCode == DeviceErrorCode.None;

    public static CardVerifyResult Match(CardIdentity intended, CardIdentity actual) =>
        new(true, intended, actual, DeviceErrorCode.None, "Verified");

    public static CardVerifyResult Mismatch(CardIdentity intended, CardIdentity actual) =>
        new(false, intended, actual, DeviceErrorCode.VerificationFailed, "Read-back EPC does not match intended identity.");

    public static CardVerifyResult Fail(CardIdentity intended, DeviceErrorCode errorCode, string message) =>
        new(false, intended, null, errorCode, message);
}
