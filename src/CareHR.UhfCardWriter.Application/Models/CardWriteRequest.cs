using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Request to write an intended CareHR identity to a physical card (UC-007).</summary>
public sealed class CardWriteRequest
{
    public CardWriteRequest(CardIdentity intendedIdentity, byte[] accessPassword)
    {
        IntendedIdentity = intendedIdentity ?? throw new ArgumentNullException(nameof(intendedIdentity));
        AccessPassword = accessPassword ?? throw new ArgumentNullException(nameof(accessPassword));
    }

    public CardIdentity IntendedIdentity { get; }

    /// <summary>Gen2 access password (exactly four bytes).</summary>
    public byte[] AccessPassword { get; }
}
