namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Outcome of reading card memory/identity.
/// </summary>
public sealed class CardReadResult
{
    public CardReadResult(
        byte status,
        byte antenna,
        byte[] crc,
        byte[] pc,
        byte[] code,
        byte wordCount,
        byte[] data)
    {
        Status = status;
        Antenna = antenna;
        Crc = crc ?? Array.Empty<byte>();
        Pc = pc ?? Array.Empty<byte>();
        Code = code ?? Array.Empty<byte>();
        WordCount = wordCount;
        Data = data ?? Array.Empty<byte>();
    }

    public byte Status { get; }

    public byte Antenna { get; }

    public byte[] Crc { get; }

    public byte[] Pc { get; }

    public byte[] Code { get; }

    public byte WordCount { get; }

    public byte[] Data { get; }
}
