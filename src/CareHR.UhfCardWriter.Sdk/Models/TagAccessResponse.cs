namespace CareHR.UhfCardWriter.Sdk.Models;

/// <summary>
/// Managed access-command response (write/lock/kill style TagResp).
/// </summary>
public sealed class TagAccessResponse
{
    /// <summary>Initializes an access response.</summary>
    /// <param name="tagStatus">Tag status byte from the reader.</param>
    /// <param name="antenna">Antenna id.</param>
    /// <param name="crc">CRC bytes.</param>
    /// <param name="pc">PC bytes.</param>
    /// <param name="code">Code/UII bytes from the response.</param>
    public TagAccessResponse(
        byte tagStatus,
        byte antenna,
        byte[] crc,
        byte[] pc,
        byte[] code)
    {
        TagStatus = tagStatus;
        Antenna = antenna;
        Crc = crc ?? Array.Empty<byte>();
        Pc = pc ?? Array.Empty<byte>();
        Code = code ?? Array.Empty<byte>();
    }

    /// <summary>Gets the tag status byte.</summary>
    public byte TagStatus { get; }

    /// <summary>Gets the antenna id.</summary>
    public byte Antenna { get; }

    /// <summary>Gets CRC bytes.</summary>
    public byte[] Crc { get; }

    /// <summary>Gets PC bytes.</summary>
    public byte[] Pc { get; }

    /// <summary>Gets code/UII bytes.</summary>
    public byte[] Code { get; }
}
