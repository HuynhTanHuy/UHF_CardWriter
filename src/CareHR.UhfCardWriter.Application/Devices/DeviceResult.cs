namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Application-facing result of a single device operation.
/// </summary>
/// <remarks>
/// Uses <see cref="DeviceErrorCode"/> — not vendor <c>STAT_*</c> integers.
/// Check <see cref="Success"/> before using payloads on <see cref="DeviceResult{T}"/>.
/// </remarks>
public readonly struct DeviceResult
{
    public DeviceErrorCode ErrorCode { get; }

    public bool Success { get; }

    public string Message { get; }

    /// <param name="errorCode">Application error code.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="message">Human-readable message.</param>
    public DeviceResult(DeviceErrorCode errorCode, bool success, string message)
    {
        ErrorCode = errorCode;
        Success = success;
        Message = message ?? string.Empty;
    }

    public static DeviceResult Ok(string message = "OK") =>
        new(DeviceErrorCode.None, true, message);

    public static DeviceResult Fail(DeviceErrorCode errorCode, string message) =>
        new(errorCode, false, message);
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

    public DeviceResult(DeviceErrorCode errorCode, bool success, string message, T? value)
    {
        ErrorCode = errorCode;
        Success = success;
        Message = message ?? string.Empty;
        Value = value;
    }

    public static DeviceResult<T> Ok(T value, string message = "OK") =>
        new(DeviceErrorCode.None, true, message, value);

    public static DeviceResult<T> Fail(DeviceErrorCode errorCode, string message) =>
        new(errorCode, false, message, default);
}
