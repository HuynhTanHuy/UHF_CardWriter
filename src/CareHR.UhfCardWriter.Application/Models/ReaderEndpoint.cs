namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>How the Operator reaches a desk reader.</summary>
public enum ReaderConnectionKind
{
    Serial,
    UsbHid,
    Network,
}

/// <summary>
/// Endpoint describing how to open a reader session.
/// </summary>
public sealed class ReaderEndpoint
{
    private ReaderEndpoint(ReaderConnectionKind kind)
    {
        Kind = kind;
    }

    public ReaderConnectionKind Kind { get; }

    /// <summary>COM port name when <see cref="Kind"/> is Serial.</summary>
    public string? ComPort { get; private init; }

    /// <summary>Baud rate when <see cref="Kind"/> is Serial.</summary>
    public int BaudRate { get; private init; }

    /// <summary>USB HID device index when <see cref="Kind"/> is UsbHid.</summary>
    public ushort UsbIndex { get; private init; }

    /// <summary>IP address when <see cref="Kind"/> is Network.</summary>
    public string? IpAddress { get; private init; }

    /// <summary>TCP port when <see cref="Kind"/> is Network.</summary>
    public ushort NetworkPort { get; private init; }

    /// <summary>Connect timeout (ms) when <see cref="Kind"/> is Network.</summary>
    public int NetworkTimeoutMs { get; private init; }

    public static ReaderEndpoint Serial(string comPort, int baudRate) =>
        new(ReaderConnectionKind.Serial)
        {
            ComPort = comPort,
            BaudRate = baudRate,
        };

    public static ReaderEndpoint UsbHid(ushort index) =>
        new(ReaderConnectionKind.UsbHid) { UsbIndex = index };

    public static ReaderEndpoint Network(string ipAddress, ushort port, int timeoutMs) =>
        new(ReaderConnectionKind.Network)
        {
            IpAddress = ipAddress,
            NetworkPort = port,
            NetworkTimeoutMs = timeoutMs,
        };
}
