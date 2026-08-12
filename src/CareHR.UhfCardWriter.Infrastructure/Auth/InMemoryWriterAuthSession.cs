using CareHR.UhfCardWriter.Application.Abstractions;

namespace CareHR.UhfCardWriter.Infrastructure.Auth;

/// <summary>Thread-safe in-memory JWT holder (no persistence).</summary>
public sealed class InMemoryWriterAuthSession : IWriterAuthSession
{
    private readonly object _gate = new();
    private string? _token;

    public void SetToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required.", nameof(token));

        lock (_gate)
            _token = token.Trim();
    }

    public bool TryGetToken(out string token)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                token = string.Empty;
                return false;
            }

            token = _token;
            return true;
        }
    }

    public void ClearToken()
    {
        lock (_gate)
            _token = null;
    }

    public bool HasToken
    {
        get
        {
            lock (_gate)
                return !string.IsNullOrWhiteSpace(_token);
        }
    }
}
