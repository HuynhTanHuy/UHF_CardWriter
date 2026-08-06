namespace CareHR.UhfCardWriter.Sdk.Driver;

/// <summary>
/// Managed inventory identity converted from native TagInfo.
/// </summary>
/// <remarks>Not a native struct. Produced by <see cref="UhfPrimeDriver.GetTagUii"/>.</remarks>
public sealed class TagIdentityNative
{
    /// <summary>Initializes a managed tag identity.</summary>
    /// <param name="no">Tag sequence number from SDK.</param>
    /// <param name="rssiTenthsDbm">RSSI in 0.1 dBm units.</param>
    /// <param name="antenna">Antenna id.</param>
    /// <param name="channel">Channel id.</param>
    /// <param name="crc">CRC bytes (copy).</param>
    /// <param name="pc">PC bytes (copy).</param>
    /// <param name="epc">EPC/UII bytes (copy).</param>
    public TagIdentityNative(
        ushort no,
        short rssiTenthsDbm,
        byte antenna,
        byte channel,
        byte[] crc,
        byte[] pc,
        byte[] epc)
    {
        NO = no;
        RssiTenthsDbm = rssiTenthsDbm;
        Antenna = antenna;
        Channel = channel;
        Crc = crc ?? Array.Empty<byte>();
        Pc = pc ?? Array.Empty<byte>();
        Epc = epc ?? Array.Empty<byte>();
    }

    /// <summary>Gets the SDK tag sequence number.</summary>
    public ushort NO { get; }

    /// <summary>Gets RSSI in 0.1 dBm units (SDK).</summary>
    public short RssiTenthsDbm { get; }

    /// <summary>Gets the antenna id.</summary>
    public byte Antenna { get; }

    /// <summary>Gets the channel id.</summary>
    public byte Channel { get; }

    /// <summary>Gets CRC bytes.</summary>
    public byte[] Crc { get; }

    /// <summary>Gets PC bytes.</summary>
    public byte[] Pc { get; }

    /// <summary>Gets raw EPC/UII bytes (length = CodeLength from SDK).</summary>
    public byte[] Epc { get; }

    /// <summary>Gets <see cref="Epc"/> encoded as uppercase hex without separators.</summary>
    public string EpcHex => Convert.ToHexString(Epc);
}
