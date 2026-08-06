using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Request to write an intended CareHR identity to a physical card (UC-007).</summary>
public sealed class CardWriteRequest
{
    /// <summary>Initializes a write request.</summary>
    public CardWriteRequest(CardIdentity intendedIdentity, byte[] accessPassword)
    {
        IntendedIdentity = intendedIdentity ?? throw new ArgumentNullException(nameof(intendedIdentity));
        AccessPassword = accessPassword ?? throw new ArgumentNullException(nameof(accessPassword));
    }

    /// <summary>Gets the intended identity to write.</summary>
    public CardIdentity IntendedIdentity { get; }

    /// <summary>Gets the Gen2 access password (exactly four bytes).</summary>
    public byte[] AccessPassword { get; }
}
