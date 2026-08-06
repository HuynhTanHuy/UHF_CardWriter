namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Outcome of reading card memory/identity.
/// </summary>
public sealed class CardReadResult
{
    /// <summary>Initializes a read result.</summary>
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

    /// <summary>Gets the device status byte from the access response.</summary>
    public byte Status { get; }

    /// <summary>Gets the antenna id.</summary>
    public byte Antenna { get; }

    /// <summary>Gets CRC bytes.</summary>
    public byte[] Crc { get; }

    /// <summary>Gets PC bytes.</summary>
    public byte[] Pc { get; }

    /// <summary>Gets code bytes from the access response.</summary>
    public byte[] Code { get; }

    /// <summary>Gets the word count reported by the device.</summary>
    public byte WordCount { get; }

    /// <summary>Gets payload bytes.</summary>
    public byte[] Data { get; }
}
