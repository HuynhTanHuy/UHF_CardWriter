using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// Application port for scanning and selecting a CareHR card in the RF field.
/// </summary>
/// <remarks>Single-call primitives only — no inventory poll loop.</remarks>
public interface ICardScanner
{
    /// <summary>Starts RF scanning (single continue command).</summary>
    DeviceResult StartScan(byte invCount = 0, uint invParam = 0);

    DeviceResult StopScan(ushort timeoutMs = DeviceConstants.DefaultInventoryStopTimeoutMs);

    /// <summary>Tries to obtain one card from the field (single poll).</summary>
    DeviceResult<CardInformation> TryGetCard(ushort timeoutMs);

    /// <summary>Selects a card by identity (EPC mask) for subsequent access.</summary>
    /// <param name="identity">Card identity whose EPC bytes form the select mask.</param>
    DeviceResult SelectByIdentity(CardIdentity identity);
}
