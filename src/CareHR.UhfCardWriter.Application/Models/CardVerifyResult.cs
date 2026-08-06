using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Result of UC-008 Verify Card.</summary>
public sealed class CardVerifyResult
{
    /// <summary>Initializes a verify result.</summary>
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

    /// <summary>Gets whether read-back matches the intended identity.</summary>
    public bool IsMatch { get; }

    /// <summary>Gets the intended identity.</summary>
    public CardIdentity IntendedIdentity { get; }

    /// <summary>Gets the identity read from the card when available.</summary>
    public CardIdentity? ActualIdentity { get; }

    /// <summary>Gets the Application error code.</summary>
    public DeviceErrorCode ErrorCode { get; }

    /// <summary>Gets a human-readable message.</summary>
    public string Message { get; }

    /// <summary>Gets whether verify succeeded (match).</summary>
    public bool Success => IsMatch && ErrorCode == DeviceErrorCode.None;

    /// <summary>Creates a successful match result.</summary>
    public static CardVerifyResult Match(CardIdentity intended, CardIdentity actual) =>
        new(true, intended, actual, DeviceErrorCode.None, "Verified");

    /// <summary>Creates a mismatch result.</summary>
    public static CardVerifyResult Mismatch(CardIdentity intended, CardIdentity actual) =>
        new(false, intended, actual, DeviceErrorCode.VerificationFailed, "Read-back EPC does not match intended identity.");

    /// <summary>Creates a verify failure when read failed.</summary>
    public static CardVerifyResult Fail(CardIdentity intended, DeviceErrorCode errorCode, string message) =>
        new(false, intended, null, errorCode, message);
}
