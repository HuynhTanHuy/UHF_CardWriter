namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Outcome of writing identity to a physical card.
/// </summary>
public sealed class CardWriteResult
{
    /// <summary>Initializes a write result.</summary>
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

    /// <summary>Gets the device status byte from the access response.</summary>
    public byte Status { get; }

    /// <summary>Gets the antenna id.</summary>
    public byte Antenna { get; }

    /// <summary>Gets CRC bytes.</summary>
    public byte[] Crc { get; }

    /// <summary>Gets PC bytes.</summary>
    public byte[] Pc { get; }

    /// <summary>Gets code/identity bytes returned in the access response.</summary>
    public byte[] Code { get; }
}
