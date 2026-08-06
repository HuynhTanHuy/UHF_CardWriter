using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Sdk;

/// <summary>
/// Single-call inventory primitives (no polling loop).
/// </summary>
public interface IUhfInventory
{
    /// <summary>Issues a single inventory-continue command.</summary>
    /// <param name="invCount">SDK inventory count parameter.</param>
    /// <param name="invParam">SDK inventory parameter.</param>
    /// <returns>SDK result.</returns>
    /// <remarks>Does not loop. Caller decides when to call <see cref="GetCurrentTag"/> / <see cref="Stop"/>.</remarks>
    SdkResult Start(byte invCount = 0, uint invParam = 0);

    /// <summary>Issues a single inventory-stop command.</summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>SDK result.</returns>
    SdkResult Stop(ushort timeoutMs = SdkConstants.DefaultInventoryStopTimeoutMs);

    /// <summary>Polls one tag identity from the reader.</summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>SDK result with <see cref="TagIdentity"/> on success.</returns>
    /// <remarks>Single poll only — not an inventory loop.</remarks>
    SdkResult<TagIdentity> GetCurrentTag(ushort timeoutMs);
}
