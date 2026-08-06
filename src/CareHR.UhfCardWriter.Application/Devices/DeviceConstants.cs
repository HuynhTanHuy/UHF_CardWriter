namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Application-facing device constants (timeouts / password length).
/// </summary>
public static class DeviceConstants
{
    /// <summary>Required Gen2 access password length in bytes.</summary>
    public const int AccessPasswordLength = 4;

    /// <summary>Default inventory stop timeout (ms).</summary>
    public const ushort DefaultInventoryStopTimeoutMs = 10000;

    /// <summary>Default write response timeout (ms).</summary>
    public const ushort DefaultWriteResponseTimeoutMs = 1000;

    /// <summary>Default read response timeout (ms).</summary>
    public const ushort DefaultReadResponseTimeoutMs = 1000;

    /// <summary>Default USB info string capacity.</summary>
    public const int DefaultUsbInfoCapacity = 256;

    /// <summary>Default scan window to detect a single card (ms).</summary>
    public const ushort DefaultScanTimeoutMs = 3000;

    /// <summary>Polling interval inside a scan window (ms).</summary>
    public const int DefaultScanPollIntervalMs = 50;
}
