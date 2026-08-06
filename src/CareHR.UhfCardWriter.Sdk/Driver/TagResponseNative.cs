namespace CareHR.UhfCardWriter.Sdk.Driver;

/// <summary>
/// Managed access response converted from native TagResp.
/// </summary>
/// <remarks>Not a native struct. Produced by <see cref="UhfPrimeDriver.GetTagResp"/>.</remarks>
public sealed class TagResponseNative
{
    /// <summary>Initializes a managed tag response.</summary>
    /// <param name="tagStatus">Tag/status byte from SDK.</param>
    /// <param name="antenna">Antenna id.</param>
    /// <param name="crc">CRC bytes (copy).</param>
    /// <param name="pc">PC bytes (copy).</param>
    /// <param name="code">Code/UII bytes (copy).</param>
    public TagResponseNative(
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

    /// <summary>Gets the tag status byte from the SDK response.</summary>
    public byte TagStatus { get; }

    /// <summary>Gets the antenna id.</summary>
    public byte Antenna { get; }

    /// <summary>Gets CRC bytes.</summary>
    public byte[] Crc { get; }

    /// <summary>Gets PC bytes.</summary>
    public byte[] Pc { get; }

    /// <summary>Gets code/UII bytes from the response.</summary>
    public byte[] Code { get; }
}

/// <summary>ReadTag response: TagResp metadata plus memory words as bytes.</summary>
/// <remarks>Produced by <see cref="UhfPrimeDriver.GetReadTagResp"/>. <see cref="Data"/> is an independent copy.</remarks>
public sealed class TagReadNative
{
    /// <summary>Initializes a read response payload.</summary>
    /// <param name="response">Marshaled TagResp metadata.</param>
    /// <param name="wordCount">Word count reported by the SDK.</param>
    /// <param name="data">Payload bytes (copy).</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is null.</exception>
    public TagReadNative(TagResponseNative response, byte wordCount, byte[] data)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
        WordCount = wordCount;
        Data = data ?? Array.Empty<byte>();
    }

    /// <summary>Gets the marshaled TagResp metadata.</summary>
    public TagResponseNative Response { get; }

    /// <summary>Gets the word count reported by the SDK.</summary>
    public byte WordCount { get; }

    /// <summary>Gets the read payload bytes (caller-owned copy).</summary>
    public byte[] Data { get; }
}
