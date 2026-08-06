using CareHR.UhfCardWriter.Sdk.Native;

namespace CareHR.UhfCardWriter.Sdk.Driver;

/// <summary>
/// Result of a single native SDK call. SDK status codes are not thrown.
/// </summary>
/// <remarks>
/// Check <see cref="Success"/> (or <see cref="StatusCode"/>) before treating the operation as completed successfully.
/// See docs/NativeResultReview.md and docs/ExceptionPolicy.md.
/// </remarks>
public readonly struct NativeResult
{
    /// <summary>Gets the raw SDK <c>STAT_*</c> status code.</summary>
    public int StatusCode { get; }

    /// <summary>Gets the raw SDK status (alias of <see cref="StatusCode"/>).</summary>
    public int NativeStatus => StatusCode;

    /// <summary>Gets a value indicating whether <see cref="StatusCode"/> equals OK (0).</summary>
    public bool Success => StatusCode == NativeConstants.StatOk;

    /// <summary>Gets a human-readable description of <see cref="StatusCode"/>.</summary>
    public string Message { get; }

    /// <summary>Initializes a new <see cref="NativeResult"/>.</summary>
    /// <param name="statusCode">SDK status code.</param>
    /// <param name="message">Optional message; when null, <see cref="Describe"/> is used.</param>
    public NativeResult(int statusCode, string? message = null)
    {
        StatusCode = statusCode;
        Message = message ?? Describe(statusCode);
    }

    /// <summary>Creates a successful result.</summary>
    /// <returns>Result with status OK.</returns>
    public static NativeResult Ok() => new(NativeConstants.StatOk, "OK");

    /// <summary>Creates a result from an SDK status code.</summary>
    /// <param name="statusCode">SDK status code.</param>
    /// <returns>Mapped result.</returns>
    public static NativeResult FromStatus(int statusCode) => new(statusCode);

    /// <summary>Maps a known SDK status code to a short English message.</summary>
    /// <param name="statusCode">SDK status code.</param>
    /// <returns>Description, or a hex fallback for unknown codes.</returns>
    public static string Describe(int statusCode) => statusCode switch
    {
        NativeConstants.StatOk => "OK",
        NativeConstants.StatPortHandleErr => "Port handle error",
        NativeConstants.StatPortOpenFailed => "Open port failed",
        NativeConstants.StatDllInnerFailed => "DLL internal error",
        NativeConstants.StatCmdParamErr => "Parameter error",
        NativeConstants.StatCmdInnerErr => "Module internal error",
        NativeConstants.StatCmdInventoryStop => "Inventory stopped / no tag",
        NativeConstants.StatCmdTagNoResp => "Tag no response",
        NativeConstants.StatCmdPwdErr => "Password error",
        NativeConstants.StatCmdAuthFail => "Authentication failed",
        NativeConstants.StatCmdRespFormatErr => "Response format error",
        NativeConstants.StatCmdCommTimeout => "Communication timeout",
        NativeConstants.StatCmdNomoreData => "No more data",
        NativeConstants.StatDllUnconnect => "Not connected",
        NativeConstants.StatDllDisconnect => "Disconnected",
        NativeConstants.StatIsoTagMemLck => "Tag memory locked",
        NativeConstants.StatIsoTagOprLimit => "Tag operation permission denied",
        _ => $"SDK status 0x{statusCode:X8}"
    };
}

/// <summary>
/// <see cref="NativeResult"/> with a managed payload (never a native struct).
/// </summary>
/// <typeparam name="T">Managed payload type.</typeparam>
/// <remarks>
/// When <see cref="Success"/> is false, <see cref="Value"/> is undefined and should not be used.
/// </remarks>
public readonly struct NativeResult<T>
{
    /// <summary>Gets the raw SDK <c>STAT_*</c> status code.</summary>
    public int StatusCode { get; }

    /// <summary>Gets the raw SDK status (alias of <see cref="StatusCode"/>).</summary>
    public int NativeStatus => StatusCode;

    /// <summary>Gets a value indicating whether <see cref="StatusCode"/> equals OK (0).</summary>
    public bool Success => StatusCode == NativeConstants.StatOk;

    /// <summary>Gets a human-readable description of <see cref="StatusCode"/>.</summary>
    public string Message { get; }

    /// <summary>Gets the managed payload when <see cref="Success"/> is true; otherwise undefined.</summary>
    public T? Value { get; }

    /// <summary>Initializes a new <see cref="NativeResult{T}"/>.</summary>
    /// <param name="statusCode">SDK status code.</param>
    /// <param name="value">Managed payload.</param>
    /// <param name="message">Optional message; when null, <see cref="NativeResult.Describe"/> is used.</param>
    public NativeResult(int statusCode, T? value, string? message = null)
    {
        StatusCode = statusCode;
        Value = value;
        Message = message ?? NativeResult.Describe(statusCode);
    }

    /// <summary>Creates a successful result with a payload.</summary>
    /// <param name="value">Managed payload.</param>
    /// <returns>OK result.</returns>
    public static NativeResult<T> Ok(T value) =>
        new(NativeConstants.StatOk, value, "OK");

    /// <summary>Creates a failed/non-OK result without a meaningful payload.</summary>
    /// <param name="statusCode">SDK status code.</param>
    /// <returns>Result with default <see cref="Value"/>.</returns>
    public static NativeResult<T> FromStatus(int statusCode) =>
        new(statusCode, default);
}
