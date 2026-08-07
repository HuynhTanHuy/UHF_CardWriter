using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// Application port for writing CareHR card identity (EPC) to a physical card.
/// </summary>
/// <remarks>Gen2 memory-bank details are handled by Infrastructure — not exposed here.</remarks>
public interface ICardWriter
{
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="epcPayload">Non-empty even-length EPC payload bytes.</param>
    DeviceResult<CardWriteResult> WriteEpc(
        byte[] accessPassword,
        byte[] epcPayload,
        ushort responseTimeoutMs = DeviceConstants.DefaultWriteResponseTimeoutMs);
}
