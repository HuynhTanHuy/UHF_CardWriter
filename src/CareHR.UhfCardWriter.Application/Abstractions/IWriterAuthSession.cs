namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// In-memory CareHR JWT session for API calls (set by Writer LoginForm).
/// Token is never persisted to disk or config.
/// </summary>
public interface IWriterAuthSession
{
    void SetToken(string token);

    bool TryGetToken(out string token);

    void ClearToken();

    bool HasToken { get; }
}
