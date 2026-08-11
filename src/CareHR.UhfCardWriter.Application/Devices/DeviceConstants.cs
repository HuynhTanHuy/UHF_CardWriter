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

    // Vendor Form1 DevicePara.INTERFACE / sample port mapping (confirmed GET values).
    public const byte OutInterfaceUsb = 0x01;
    public const byte OutInterfaceKeyBoard = 0x02;
    public const byte OutInterfaceCdcCom = 0x04;
    public const byte OutInterfaceWiFi = 0x10;
    public const byte OutInterfaceRj45 = 0x20;
    public const byte OutInterfaceRs485 = 0x40;
    public const byte OutInterfaceRs232 = 0x80;

    /// <summary>Display names for vendor-confirmed Out Interface values.</summary>
    public static string FormatOutInterface(byte raw) =>
        raw switch
        {
            OutInterfaceUsb => "USB",
            OutInterfaceKeyBoard => "KeyBoard",
            OutInterfaceCdcCom => "CDC_COM",
            OutInterfaceWiFi => "WiFi",
            OutInterfaceRj45 => "RJ45",
            OutInterfaceRs485 => "RS485",
            OutInterfaceRs232 => "RS232",
            _ => $"Unknown(0x{raw:X2})",
        };

    /// <summary>Vendor-confirmed Out Interface options for UI (name → raw).</summary>
    public static IReadOnlyList<(string Name, byte Raw)> OutInterfaceOptions { get; } =
    [
        ("USB", OutInterfaceUsb),
        ("KeyBoard", OutInterfaceKeyBoard),
        ("CDC_COM", OutInterfaceCdcCom),
        ("WiFi", OutInterfaceWiFi),
        ("RJ45", OutInterfaceRj45),
        ("RS485", OutInterfaceRs485),
        ("RS232", OutInterfaceRs232),
    ];

    /// <summary>
    /// Vendor Desk Reader sample <c>cmbTxPower</c> / CareHR RfidGateway range (dBm).
    /// Unit confirmed by vendor UI label "RfPower（dbm）".
    /// </summary>
    public const byte RfPowerMinDbm = 0;

    /// <summary>Vendor max RF power in dBm (cmbTxPower items 0..33).</summary>
    public const byte RfPowerMaxDbm = 33;

    /// <summary>True when <paramref name="power"/> is within vendor UI range 0..33 dBm.</summary>
    public static bool IsValidRfPowerDbm(byte power) =>
        power is >= RfPowerMinDbm and <= RfPowerMaxDbm;
}
