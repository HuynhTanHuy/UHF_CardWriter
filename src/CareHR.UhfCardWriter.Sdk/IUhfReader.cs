using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Sdk;

/// <summary>
/// Gen2 read access surface.
/// </summary>
public interface IUhfReader
{
    /// <summary>
    /// Issues a read command, then fetches the read payload when the read command succeeds.
    /// </summary>
    /// <param name="option">Vendor read option byte.</param>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="memBank">Target Gen2 memory bank.</param>
    /// <param name="wordPtr">Starting word pointer.</param>
    /// <param name="wordCount">Number of words to read.</param>
    /// <param name="responseTimeoutMs">Timeout for the read response.</param>
    /// <returns>SDK result with <see cref="TagReadData"/> when both steps succeed.</returns>
    /// <remarks>Does not verify against an expected value.</remarks>
    SdkResult<TagReadData> Read(
        byte option,
        byte[] accessPassword,
        MemBank memBank,
        ushort wordPtr,
        byte wordCount,
        ushort responseTimeoutMs = SdkConstants.DefaultReadResponseTimeoutMs);
}
