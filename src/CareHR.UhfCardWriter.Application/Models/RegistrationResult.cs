using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Result of UC-009 Register Card.</summary>
public sealed class RegistrationResult
{
    /// <summary>Initializes a registration result.</summary>
    public RegistrationResult(bool success, DeviceErrorCode errorCode, string message)
    {
        Success = success;
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
    }

    /// <summary>Gets whether registration succeeded.</summary>
    public bool Success { get; }

    /// <summary>Gets the Application error code.</summary>
    public DeviceErrorCode ErrorCode { get; }

    /// <summary>Gets a human-readable message.</summary>
    public string Message { get; }

    /// <summary>Creates a successful registration result.</summary>
    public static RegistrationResult Ok(string message = "Registered") =>
        new(true, DeviceErrorCode.None, message);

    /// <summary>Creates a failed registration result.</summary>
    public static RegistrationResult Fail(DeviceErrorCode errorCode, string message) =>
        new(false, errorCode, message);
}
