namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Card identity plus scan metadata for CareHR card issuance.
/// </summary>
public sealed class CardInformation
{
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

    public CardIdentity Identity { get; }

    public ushort SequenceNumber { get; }

    public short RssiTenthsDbm { get; }

    public byte Antenna { get; }

    public byte Channel { get; }

    public byte[] Crc { get; }

    public byte[] Pc { get; }
}
