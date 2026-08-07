using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Request to register a verified card with CareHR (UC-009).</summary>
public sealed class RegistrationRequest
{
    /// <summary>Initializes a registration request.</summary>
    public RegistrationRequest(
        CardIdentity identity,
        string hospitalId,
        string cardTypeId,
        string batchCode,
        bool isVerified)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        HospitalId = hospitalId ?? string.Empty;
        CardTypeId = cardTypeId ?? string.Empty;
        BatchCode = batchCode ?? string.Empty;
        IsVerified = isVerified;
    }

    /// <summary>Gets the verified card identity.</summary>
    public CardIdentity Identity { get; }

    /// <summary>Gets the CareHR hospital id (<c>hospitalId</c>).</summary>
    public string HospitalId { get; }

    /// <summary>Gets the CareHR RFID card type id.</summary>
    public string CardTypeId { get; }

    /// <summary>Gets the CareHR RFID card batch code.</summary>
    public string BatchCode { get; }

    /// <summary>Gets whether Application has verified the physical card (BR-004).</summary>
    public bool IsVerified { get; }
}
