namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Business identity of a CareHR card (EPC).
/// </summary>
public sealed class CardIdentity
{
    /// <summary>Initializes a card identity from EPC bytes.</summary>
    /// <param name="epc">Raw EPC/UII bytes.</param>
    public CardIdentity(byte[] epc)
    {
        Epc = epc ?? Array.Empty<byte>();
    }

    /// <summary>Gets raw EPC bytes.</summary>
    public byte[] Epc { get; }

    /// <summary>Gets EPC as uppercase hex without separators.</summary>
    public string EpcHex => Convert.ToHexString(Epc);
}
