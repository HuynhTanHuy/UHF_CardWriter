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
}
