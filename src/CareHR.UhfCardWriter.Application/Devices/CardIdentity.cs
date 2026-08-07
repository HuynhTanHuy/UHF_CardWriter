namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Business identity of a CareHR card (EPC).
/// </summary>
public sealed class CardIdentity
{
    public CardIdentity(byte[] epc)
    {
        Epc = epc ?? Array.Empty<byte>();
    }

    public byte[] Epc { get; }

    /// <summary>Uppercase hex without separators (registry / UI display form).</summary>
    public string EpcHex => Convert.ToHexString(Epc);
}
