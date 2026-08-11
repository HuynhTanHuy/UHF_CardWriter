using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// Application port for persisting a verified CareHR card identity to the backend registry.
/// </summary>
/// <remarks>HTTP/OData details belong in Infrastructure — not here.</remarks>
public interface ICardRegistrar
{
    /// <summary>
    /// Registers a verified card identity with CareHR (type + batch metadata).
    /// </summary>
    /// <param name="request">Registration request (identity must already be verified by Application).</param>
    RegistrationResult Register(RegistrationRequest request);

    /// <summary>
    /// Checks whether <paramref name="rfidCardNumber"/> already exists for the hospital
    /// (exact match on CareHR <c>RFIDCardNumber</c>).
    /// </summary>
    CardExistenceResult Exists(string hospitalId, string rfidCardNumber);

    /// <summary>
    /// Resolves next serial for <paramref name="numberPrefix"/> (HospitalNumber + Batch D2)
    /// as MAX(matching serial) + 1 within the same hospital. Empty set → 1.
    /// </summary>
    /// <param name="hospitalId">Hospital GUID (UI / JWT hospital).</param>
    /// <param name="numberPrefix">Exact prefix, e.g. <c>7904801</c>.</param>
    /// <param name="serialWidth">Serial digit width (CardWritter D5 = 5).</param>
    NextSerialResult GetNextSerial(string hospitalId, string numberPrefix, int serialWidth);
}
