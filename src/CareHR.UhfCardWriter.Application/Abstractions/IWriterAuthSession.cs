namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// In-memory CareHR JWT session for API registration (set via local bridge from frontend).
/// </summary>
public interface IWriterAuthSession
{
    void SetToken(string token);

    bool TryGetToken(out string token);

    void ClearToken();

    bool HasToken { get; }
}
