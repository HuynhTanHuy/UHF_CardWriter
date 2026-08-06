using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Sdk;

/// <summary>
/// Gen2 write access surface.
/// </summary>
public interface IUhfWriter
{
    /// <summary>
    /// Writes memory words, then fetches the write access response when the write command succeeds.
    /// </summary>
    /// <param name="option">Vendor write option byte.</param>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="memBank">Target Gen2 memory bank.</param>
    /// <param name="wordPtr">Starting word pointer.</param>
    /// <param name="writeData">Non-empty even-length payload.</param>
    /// <param name="responseTimeoutMs">Timeout for the access response.</param>
    /// <returns>SDK result with <see cref="TagAccessResponse"/> when both steps succeed.</returns>
    /// <remarks>Does not select, verify, stop inventory, or retry.</remarks>
    SdkResult<TagAccessResponse> Write(
        byte option,
        byte[] accessPassword,
        MemBank memBank,
        ushort wordPtr,
        byte[] writeData,
        ushort responseTimeoutMs = SdkConstants.DefaultWriteResponseTimeoutMs);
}
