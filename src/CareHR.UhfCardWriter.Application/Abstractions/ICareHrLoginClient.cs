using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>CareHR <c>POST /api/auth/login</c> client (no persistence).</summary>
public interface ICareHrLoginClient
{
    Task<CareHrLoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
