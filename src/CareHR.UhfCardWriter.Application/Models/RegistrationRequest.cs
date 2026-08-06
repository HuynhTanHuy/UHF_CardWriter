using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Request to register a verified card with CareHR (UC-009).</summary>
public sealed class RegistrationRequest
{
    /// <summary>Initializes a registration request.</summary>
    public RegistrationRequest(
        CardIdentity identity,
        string cardTypeId,
        string batchCode,
        bool isVerified)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        CardTypeId = cardTypeId ?? string.Empty;
        BatchCode = batchCode ?? string.Empty;
        IsVerified = isVerified;
    }

    /// <summary>Gets the verified card identity.</summary>
    public CardIdentity Identity { get; }

    /// <summary>Gets the CareHR RFID tag type id.</summary>
    public string CardTypeId { get; }

    /// <summary>Gets the CareHR RFID tag batch code.</summary>
    public string BatchCode { get; }

    /// <summary>Gets whether Application has verified the physical card (BR-004).</summary>
    public bool IsVerified { get; }
}
