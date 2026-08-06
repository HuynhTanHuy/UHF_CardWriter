namespace CareHR.UhfCardWriter.Sdk.Models;

/// <summary>
/// Managed read result: access metadata plus memory payload bytes.
/// </summary>
public sealed class TagReadData
{
    /// <summary>Initializes a read result.</summary>
    /// <param name="response">Access response metadata.</param>
    /// <param name="wordCount">Word count reported by the reader.</param>
    /// <param name="data">Payload bytes (caller-owned copy).</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is null.</exception>
    public TagReadData(TagAccessResponse response, byte wordCount, byte[] data)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
        WordCount = wordCount;
        Data = data ?? Array.Empty<byte>();
    }

    /// <summary>Gets access response metadata.</summary>
    public TagAccessResponse Response { get; }

    /// <summary>Gets the word count reported by the reader.</summary>
    public byte WordCount { get; }

    /// <summary>Gets the read payload bytes.</summary>
    public byte[] Data { get; }
}
