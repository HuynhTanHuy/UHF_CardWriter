namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Card identity plus scan metadata for CareHR card issuance.
/// </summary>
public sealed class CardInformation
{
    /// <summary>Initializes card information from a scan/read.</summary>
    public CardInformation(
        CardIdentity identity,
        ushort sequenceNumber,
        short rssiTenthsDbm,
        byte antenna,
        byte channel,
        byte[] crc,
        byte[] pc)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        SequenceNumber = sequenceNumber;
        RssiTenthsDbm = rssiTenthsDbm;
        Antenna = antenna;
        Channel = channel;
        Crc = crc ?? Array.Empty<byte>();
        Pc = pc ?? Array.Empty<byte>();
    }

    /// <summary>Gets the card identity (EPC).</summary>
    public CardIdentity Identity { get; }

    /// <summary>Gets the reader sequence number from scan.</summary>
    public ushort SequenceNumber { get; }

    /// <summary>Gets RSSI in 0.1 dBm units.</summary>
    public short RssiTenthsDbm { get; }

    /// <summary>Gets the antenna id.</summary>
    public byte Antenna { get; }

    /// <summary>Gets the channel id.</summary>
    public byte Channel { get; }

    /// <summary>Gets CRC bytes from the air protocol response.</summary>
    public byte[] Crc { get; }

    /// <summary>Gets PC bytes from the air protocol response.</summary>
    public byte[] Pc { get; }
}
