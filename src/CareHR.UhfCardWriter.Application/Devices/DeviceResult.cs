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
    /// <summary>Gets the application error code (<see cref="DeviceErrorCode.None"/> when successful).</summary>
    public DeviceErrorCode ErrorCode { get; }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool Success { get; }

    /// <summary>Gets a human-readable status message.</summary>
    public string Message { get; }

    /// <summary>Initializes a new <see cref="DeviceResult"/>.</summary>
    /// <param name="errorCode">Application error code.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="message">Human-readable message.</param>
    public DeviceResult(DeviceErrorCode errorCode, bool success, string message)
    {
        ErrorCode = errorCode;
        Success = success;
        Message = message ?? string.Empty;
    }

    /// <summary>Creates a successful result.</summary>
    public static DeviceResult Ok(string message = "OK") =>
        new(DeviceErrorCode.None, true, message);

    /// <summary>Creates a failed result.</summary>
    public static DeviceResult Fail(DeviceErrorCode errorCode, string message) =>
        new(errorCode, false, message);
}

/// <summary>
/// <see cref="DeviceResult"/> with an Application-owned payload.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public readonly struct DeviceResult<T>
{
    /// <summary>Gets the application error code (<see cref="DeviceErrorCode.None"/> when successful).</summary>
    public DeviceErrorCode ErrorCode { get; }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool Success { get; }

    /// <summary>Gets a human-readable status message.</summary>
    public string Message { get; }

    /// <summary>Gets the payload when <see cref="Success"/> is true; otherwise undefined.</summary>
    public T? Value { get; }

    /// <summary>Initializes a new <see cref="DeviceResult{T}"/>.</summary>
    public DeviceResult(DeviceErrorCode errorCode, bool success, string message, T? value)
    {
        ErrorCode = errorCode;
        Success = success;
        Message = message ?? string.Empty;
        Value = value;
    }

    /// <summary>Creates a successful result with a payload.</summary>
    public static DeviceResult<T> Ok(T value, string message = "OK") =>
        new(DeviceErrorCode.None, true, message, value);

    /// <summary>Creates a failed result without a meaningful payload.</summary>
    public static DeviceResult<T> Fail(DeviceErrorCode errorCode, string message) =>
        new(errorCode, false, message, default);
}
