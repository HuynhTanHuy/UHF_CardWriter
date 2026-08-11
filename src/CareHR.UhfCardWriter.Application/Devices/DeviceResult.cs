namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Application-facing result of a single device operation.
/// </summary>
/// <remarks>
/// Uses <see cref="DeviceErrorCode"/> for application logic.
/// <see cref="VendorStatusCode"/> preserves raw SDK <c>STAT_*</c> for diagnostics only.
/// Check <see cref="Success"/> before using payloads on <see cref="DeviceResult{T}"/>.
/// </remarks>
public readonly struct DeviceResult
{
    public DeviceErrorCode ErrorCode { get; }

    public bool Success { get; }

    public string Message { get; }

    /// <summary>Raw vendor SDK status (0 on success). Diagnostics only.</summary>
    public int VendorStatusCode { get; }

    public DeviceResult(DeviceErrorCode errorCode, bool success, string message, int vendorStatusCode = 0)
    {
        ErrorCode = errorCode;
        Success = success;
        Message = message ?? string.Empty;
        VendorStatusCode = vendorStatusCode;
    }

    public static DeviceResult Ok(string message = "OK") =>
        new(DeviceErrorCode.None, true, message, 0);

    public static DeviceResult Fail(DeviceErrorCode errorCode, string message, int vendorStatusCode = 0) =>
        new(errorCode, false, message, vendorStatusCode);
}

/// <summary>
/// <see cref="DeviceResult"/> with an Application-owned payload.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public readonly struct DeviceResult<T>
{
    public DeviceErrorCode ErrorCode { get; }

    public bool Success { get; }

    public string Message { get; }

    public T? Value { get; }

    /// <summary>Raw vendor SDK status (0 on success). Diagnostics only.</summary>
    public int VendorStatusCode { get; }

    public DeviceResult(DeviceErrorCode errorCode, bool success, string message, T? value, int vendorStatusCode = 0)
    {
        ErrorCode = errorCode;
        Success = success;
        Message = message ?? string.Empty;
        Value = value;
        VendorStatusCode = vendorStatusCode;
    }

    public static DeviceResult<T> Ok(T value, string message = "OK", int vendorStatusCode = 0) =>
        new(DeviceErrorCode.None, true, message, value, vendorStatusCode);

    public static DeviceResult<T> Fail(DeviceErrorCode errorCode, string message, int vendorStatusCode = 0) =>
        new(errorCode, false, message, default, vendorStatusCode);
}
