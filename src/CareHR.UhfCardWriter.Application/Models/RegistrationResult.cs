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
