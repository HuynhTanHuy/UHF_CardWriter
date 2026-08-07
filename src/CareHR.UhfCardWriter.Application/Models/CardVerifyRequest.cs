using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Request to verify physical card identity against intended (UC-008).</summary>
public sealed class CardVerifyRequest
{
    public CardVerifyRequest(CardIdentity intendedIdentity, byte[] accessPassword)
    {
        IntendedIdentity = intendedIdentity ?? throw new ArgumentNullException(nameof(intendedIdentity));
        AccessPassword = accessPassword ?? throw new ArgumentNullException(nameof(accessPassword));
    }

    public CardIdentity IntendedIdentity { get; }

    public byte[] AccessPassword { get; }
}
