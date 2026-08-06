namespace CareHR.UhfCardWriter.Sdk.Models;

/// <summary>
/// Managed inventory tag identity returned by the SDK Wrapper.
/// </summary>
public sealed class TagIdentity
{
    /// <summary>Initializes a tag identity.</summary>
    /// <param name="no">SDK sequence number.</param>
    /// <param name="rssiTenthsDbm">RSSI in 0.1 dBm units.</param>
    /// <param name="antenna">Antenna id.</param>
    /// <param name="channel">Channel id.</param>
    /// <param name="crc">CRC bytes.</param>
    /// <param name="pc">PC bytes.</param>
    /// <param name="epc">EPC/UII bytes.</param>
    public TagIdentity(
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

    /// <summary>Gets RSSI in 0.1 dBm units.</summary>
    public short RssiTenthsDbm { get; }

    /// <summary>Gets the antenna id.</summary>
    public byte Antenna { get; }

    /// <summary>Gets the channel id.</summary>
    public byte Channel { get; }

    /// <summary>Gets CRC bytes.</summary>
    public byte[] Crc { get; }

    /// <summary>Gets PC bytes.</summary>
    public byte[] Pc { get; }

    /// <summary>Gets raw EPC/UII bytes.</summary>
    public byte[] Epc { get; }

    /// <summary>Gets <see cref="Epc"/> as uppercase hex without separators.</summary>
    public string EpcHex => Convert.ToHexString(Epc);
}
