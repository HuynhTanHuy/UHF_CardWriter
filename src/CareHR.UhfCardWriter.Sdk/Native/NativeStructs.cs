using System.Runtime.InteropServices;

namespace CareHR.UhfCardWriter.Sdk.Native;

/// <summary>
/// Native structs from CFApi.h / sample Class1.cs.
/// Layout: Sequential, no Pack (per SDK report). Phase 2 mapping only.
/// Size/offset expectations are enforced by <see cref="NativeLayout"/>.
/// </summary>
internal static class NativeStructs
{
    /// <summary>Marshal.SizeOf(NativeTagInfo) — Sequential, verified.</summary>
    public const int TagInfoSize = 266;

    /// <summary>Marshal.SizeOf(NativeTagResp) — Sequential, verified.</summary>
    public const int TagRespSize = 262;

    /// <summary>
    /// Marshal.SizeOf(NativeDevicePara) — Sequential sample layout (ushort freq fields).
    /// Payload fields occupy 25 bytes; CLR size is 26 (1 trailing pad).
    /// </summary>
    public const int DeviceParaSize = 26;
}

/// <summary>CFApi.h TagInfo — inventory result from GetTagUii.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeTagInfo
{
    public ushort NO;
    public short Rssi;
    public byte Antenna;
    public byte Channel;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public byte[] Crc;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public byte[] Pc;

    public byte CodeLength;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
    public byte[] Code;
}

/// <summary>CFApi.h TagResp — access command response (write/lock/kill path).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeTagResp
{
    public byte TagStatus;
    public byte Antenna;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public byte[] Crc;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public byte[] Pc;

    public byte CodeLength;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
    public byte[] Code;
}

/// <summary>
/// Device parameters — field order from CFApi.h DevicePara;
/// STRATFREI/STRATFRED/STEPFRE mapped as ushort like sample C# Devicepara (LE 2-byte).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeDevicePara
{
    public byte DeviceAddr;
    public byte RfidPro;
    public byte WorkMode;
    public byte Interface;
    public byte BaudRate;
    public byte WgSet;
    public byte Ant;
    public byte Region;
    public ushort StartFreI;
    public ushort StartFreD;
    public ushort StepFre;
    public byte Cn;
    public byte RfidPower;
    public byte InventoryArea;
    public byte QValue;
    public byte Session;
    public byte AcsAddr;
    public byte AcsDataLen;
    public byte FilterTime;
    public byte TriggleTime;
    public byte BuzzerTime;
    public byte InternalTime;
}
