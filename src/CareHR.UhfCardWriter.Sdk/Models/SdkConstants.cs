namespace CareHR.UhfCardWriter.Sdk.Models;

/// <summary>
/// Public constants for SDK Wrapper callers (timeouts / access password length).
/// </summary>
public static class SdkConstants
{
    /// <summary>Required Gen2 access password length in bytes.</summary>
    public const int AccessPasswordLength = 4;

    /// <summary>Default inventory stop timeout (ms).</summary>
    public const ushort DefaultInventoryStopTimeoutMs = 10000;

    /// <summary>Default timeout for write access response (ms).</summary>
    public const ushort DefaultWriteResponseTimeoutMs = 1000;

    /// <summary>Default timeout for read access response (ms).</summary>
    public const ushort DefaultReadResponseTimeoutMs = 1000;

    /// <summary>Default USB info string capacity.</summary>
    public const int DefaultUsbInfoCapacity = 256;
}
