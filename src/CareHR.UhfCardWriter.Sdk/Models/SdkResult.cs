namespace CareHR.UhfCardWriter.Sdk.Models;

/// <summary>
/// Result of a single SDK Wrapper call. Device/SDK status codes are not thrown.
/// </summary>
/// <remarks>
/// Check <see cref="Success"/> before treating the operation as completed successfully.
/// This type replaces Driver <c>NativeResult</c> on the public surface.
/// </remarks>
public readonly struct SdkResult
{
    /// <summary>Gets the raw vendor SDK status code.</summary>
    public int StatusCode { get; }

    /// <summary>Gets a value indicating whether <see cref="StatusCode"/> equals OK (0).</summary>
    public bool Success { get; }

    /// <summary>Gets a human-readable description of the status.</summary>
    public string Message { get; }

    /// <summary>Initializes a new <see cref="SdkResult"/>.</summary>
    /// <param name="statusCode">Vendor SDK status code.</param>
    /// <param name="success">Whether the call succeeded.</param>
    /// <param name="message">Human-readable message.</param>
    public SdkResult(int statusCode, bool success, string message)
    {
        StatusCode = statusCode;
        Success = success;
        Message = message ?? string.Empty;
    }
}

/// <summary>
/// <see cref="SdkResult"/> with a managed payload.
/// </summary>
/// <typeparam name="T">Managed payload type (never a native struct).</typeparam>
/// <remarks>When <see cref="Success"/> is false, <see cref="Value"/> is undefined.</remarks>
public readonly struct SdkResult<T>
{
    /// <summary>Gets the raw vendor SDK status code.</summary>
    public int StatusCode { get; }

    /// <summary>Gets a value indicating whether <see cref="StatusCode"/> equals OK (0).</summary>
    public bool Success { get; }

    /// <summary>Gets a human-readable description of the status.</summary>
    public string Message { get; }

    /// <summary>Gets the managed payload when <see cref="Success"/> is true; otherwise undefined.</summary>
    public T? Value { get; }

    /// <summary>Initializes a new <see cref="SdkResult{T}"/>.</summary>
    /// <param name="statusCode">Vendor SDK status code.</param>
    /// <param name="success">Whether the call succeeded.</param>
    /// <param name="message">Human-readable message.</param>
    /// <param name="value">Managed payload.</param>
    public SdkResult(int statusCode, bool success, string message, T? value)
    {
        StatusCode = statusCode;
        Success = success;
        Message = message ?? string.Empty;
        Value = value;
    }
}
