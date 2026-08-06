using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// Application port for reading CareHR card identity/content.
/// </summary>
/// <remarks>Gen2 memory-bank details are handled by Infrastructure — not exposed here.</remarks>
public interface ICardReader
{
    /// <summary>
    /// Reads EPC identity words from the card.
    /// </summary>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="wordCount">Number of EPC words to read (must be &gt; 0).</param>
    /// <param name="responseTimeoutMs">Timeout for the read response.</param>
    DeviceResult<CardReadResult> ReadEpc(
        byte[] accessPassword,
        byte wordCount,
        ushort responseTimeoutMs = DeviceConstants.DefaultReadResponseTimeoutMs);
}
