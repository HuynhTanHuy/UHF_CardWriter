using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Request to register a verified card with CareHR (UC-009).</summary>
public sealed class RegistrationRequest
{
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

    public CardIdentity Identity { get; }

    public string HospitalId { get; }

    public string CardTypeId { get; }

    public string BatchCode { get; }

    /// <summary>True only after Application verified the physical card (BR-004).</summary>
    public bool IsVerified { get; }
}
