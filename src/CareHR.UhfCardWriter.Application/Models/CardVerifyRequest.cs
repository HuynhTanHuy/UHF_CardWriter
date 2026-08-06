using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Request to verify physical card identity against intended (UC-008).</summary>
public sealed class CardVerifyRequest
{
    /// <summary>Initializes a verify request.</summary>
    public CardVerifyRequest(CardIdentity intendedIdentity, byte[] accessPassword)
    {
        IntendedIdentity = intendedIdentity ?? throw new ArgumentNullException(nameof(intendedIdentity));
        AccessPassword = accessPassword ?? throw new ArgumentNullException(nameof(accessPassword));
    }

    /// <summary>Gets the intended identity that must match the card.</summary>
    public CardIdentity IntendedIdentity { get; }

    /// <summary>Gets the access password used for read-back.</summary>
    public byte[] AccessPassword { get; }
}
