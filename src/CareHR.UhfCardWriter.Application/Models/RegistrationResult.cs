using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Result of UC-009 Register Card.</summary>
public sealed class RegistrationResult
{
    public RegistrationResult(bool success, DeviceErrorCode errorCode, string message)
    {
        Success = success;
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
    }

    public bool Success { get; }

    public DeviceErrorCode ErrorCode { get; }

    public string Message { get; }

    public static RegistrationResult Ok(string message = "Registered") =>
        new(true, DeviceErrorCode.None, message);

    public static RegistrationResult Fail(DeviceErrorCode errorCode, string message) =>
        new(false, errorCode, message);
}

/// <summary>Result of a pre-write registry existence check (scanned card number).</summary>
public sealed class CardExistenceResult
{
    public CardExistenceResult(bool querySucceeded, bool exists, string message)
    {
        QuerySucceeded = querySucceeded;
        Exists = exists;
        Message = message ?? string.Empty;
    }

    /// <summary>True when the HTTP/API query completed without transport/parse failure.</summary>
    public bool QuerySucceeded { get; }

    /// <summary>True when an exact RFIDCardNumber match was found for the hospital.</summary>
    public bool Exists { get; }

    public string Message { get; }

    public static CardExistenceResult Found(string message) =>
        new(true, true, message);

    public static CardExistenceResult NotFound(string message = "Card number not found.") =>
        new(true, false, message);

    public static CardExistenceResult Failed(string message) =>
        new(false, false, message);
}

/// <summary>Result of resolving the next serial for HospitalNumber + Batch prefix.</summary>
public sealed class NextSerialResult
{
    public NextSerialResult(bool success, int nextSerial, string message)
    {
        Success = success;
        NextSerial = nextSerial;
        Message = message ?? string.Empty;
    }

    public bool Success { get; }

    /// <summary>Next serial to use (1-based within D5 range when Success).</summary>
    public int NextSerial { get; }

    public string Message { get; }

    public static NextSerialResult Ok(int nextSerial, string message = "") =>
        new(true, nextSerial, message);

    public static NextSerialResult Fail(string message) =>
        new(false, 0, message);
}
