namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Outcome of writing identity to a physical card.
/// </summary>
public sealed class CardWriteResult
{
    public CardWriteResult(
        byte status,
        byte antenna,
        byte[] crc,
        byte[] pc,
        byte[] code)
    {
        Status = status;
        Antenna = antenna;
        Crc = crc ?? Array.Empty<byte>();
        Pc = pc ?? Array.Empty<byte>();
        Code = code ?? Array.Empty<byte>();
    }

    public byte Status { get; }

    public byte Antenna { get; }

    public byte[] Crc { get; }

    public byte[] Pc { get; }

    public byte[] Code { get; }
}
