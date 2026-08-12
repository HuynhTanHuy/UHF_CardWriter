using System.Net;
using System.Text;
using System.Text.Json;
using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Diagnostics;
using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Services;

namespace CareHR.UhfCardWriter.App.Bridge;

/// <summary>Embedded localhost HTTP bridge — JWT session only (no RFID scan).</summary>
public sealed class LocalBridgeHost : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppSettings _settings;
    private readonly CardConnectionService _connection;
    private readonly IWriterAuthSession _authSession;
    private readonly object _gate = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    public LocalBridgeHost(
        AppSettings settings,
        CardConnectionService connection,
        IWriterAuthSession authSession)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LocalBridgeHost));

            if (!_settings.Bridge.Enabled)
            {
                AppLog.Info("Bridge", "Local bridge disabled in configuration.");
                return;
            }

            if (_listener is not null)
                return;

            var host = string.IsNullOrWhiteSpace(_settings.Bridge.Host)
                ? "127.0.0.1"
                : _settings.Bridge.Host.Trim();
            var port = _settings.Bridge.Port > 0 ? _settings.Bridge.Port : 17890;

            if (!IPAddress.TryParse(host, out var ip) || !IPAddress.IsLoopback(ip))
            {
                AppLog.Warn("Bridge", $"Bridge host '{host}' is not loopback — forcing 127.0.0.1.");
                host = "127.0.0.1";
            }

            var prefix = $"http://{host}:{port}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                AppLog.Error("Bridge", "Failed to start local bridge", ex);
                listener.Close();
                return;
            }

            _listener = listener;
            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => ListenLoopAsync(_cts.Token));

            AppLog.Info("Bridge", $"Started {prefix.TrimEnd('/')}");
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (_listener is null)
                return;

            try
            {
                _cts?.Cancel();
                _listener.Stop();
            }
            catch (Exception ex)
            {
                AppLog.Warn("Bridge", $"Stop listener: {ex.Message}");
            }

            try
            {
                _loopTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Best-effort shutdown.
            }

            _listener.Close();
            _listener = null;
            _cts?.Dispose();
            _cts = null;
            _loopTask = null;

            AppLog.Info("Bridge", "Stopped");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _disposed = true;
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is { IsListening: true })
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                await HandleRequestAsync(context);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AppLog.Warn("Bridge", $"Request loop: {ex.Message}");
                if (context is not null)
                    TryClose(context, HttpStatusCode.InternalServerError);
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var path = request.Url?.AbsolutePath ?? string.Empty;
        var origin = request.Headers["Origin"];
        var method = request.HttpMethod;

        if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            WriteCorsHeaders(context.Response, origin);
            TryClose(context, HttpStatusCode.NoContent);
            return;
        }

        try
        {
            if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                AppLog.Info("Bridge", "Health");
                await WriteJsonAsync(
                    context,
                    HttpStatusCode.OK,
                    new
                    {
                        success = true,
                        connected = _connection.IsConnected,
                        authenticated = _authSession.HasToken,
                    },
                    origin);
                return;
            }

            if (string.Equals(path, "/api/auth/session", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseBearerToken(request, out var token))
                {
                    AppLog.Info("Bridge", "Authentication required");
                    await WriteJsonAsync(
                        context,
                        HttpStatusCode.Unauthorized,
                        new { success = false, message = "Authorization header is required." },
                        origin);
                    return;
                }

                if (!IsPlausibleJwt(token))
                {
                    AppLog.Info("Bridge", "Authentication required");
                    await WriteJsonAsync(
                        context,
                        HttpStatusCode.Unauthorized,
                        new { success = false, message = "Invalid authorization token format." },
                        origin);
                    return;
                }

                _authSession.SetToken(token);
                AppLog.Info("Bridge", "Auth session established");
                await WriteJsonAsync(context, HttpStatusCode.OK, new { success = true }, origin);
                return;
            }

            await WriteJsonAsync(
                context,
                HttpStatusCode.NotFound,
                new { success = false, message = "Not found." },
                origin);
        }
        catch (Exception ex)
        {
            AppLog.Error("Bridge", "Handle request failed", ex);
            await WriteJsonAsync(
                context,
                HttpStatusCode.OK,
                new { success = false, errorCode = "INTERNAL_ERROR", message = "Request failed." },
                origin);
        }
    }

    private static bool TryParseBearerToken(HttpListenerRequest request, out string token)
    {
        var authorization = request.Headers["Authorization"];
        if (string.IsNullOrWhiteSpace(authorization))
        {
            token = string.Empty;
            return false;
        }

        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            token = string.Empty;
            return false;
        }

        token = authorization.Substring(prefix.Length).Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

    private static bool IsPlausibleJwt(string token)
    {
        var parts = token.Split('.');
        return parts.Length == 3
               && parts[0].Length > 0
               && parts[1].Length > 0
               && parts[2].Length > 0;
    }

    private async Task WriteJsonAsync(
        HttpListenerContext context,
        HttpStatusCode statusCode,
        object payload,
        string? origin)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var data = Encoding.UTF8.GetBytes(json);

        WriteCorsHeaders(context.Response, origin);
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = data.Length;
        await context.Response.OutputStream.WriteAsync(data);
        context.Response.OutputStream.Close();
    }

    private void WriteCorsHeaders(HttpListenerResponse response, string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin) || !BridgeCors.IsOriginAllowed(origin, _settings.Bridge.AllowedOrigins))
            return;

        response.Headers["Access-Control-Allow-Origin"] = origin;
        response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        response.Headers["Vary"] = "Origin";
    }

    private static void TryClose(HttpListenerContext context, HttpStatusCode statusCode)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.Close();
    }
}

internal static class BridgeCors
{
    public static bool IsOriginAllowed(string origin, IReadOnlyList<string> allowedPatterns)
    {
        if (allowedPatterns.Count == 0)
            return false;

        foreach (var pattern in allowedPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            var trimmed = pattern.Trim();
            if (string.Equals(origin, trimmed, StringComparison.OrdinalIgnoreCase))
                return true;

            if (trimmed.Contains('*') && MatchesWildcardOrigin(origin, trimmed))
                return true;
        }

        return false;
    }

    private static bool MatchesWildcardOrigin(string origin, string pattern)
    {
        const string delimiter = "://*";
        var idx = pattern.IndexOf(delimiter, StringComparison.Ordinal);
        if (idx < 0)
            return false;

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            return false;

        var scheme = pattern[..idx];
        var hostSuffix = pattern[(idx + delimiter.Length)..];
        if (!string.Equals(originUri.Scheme, scheme, StringComparison.OrdinalIgnoreCase))
            return false;

        var host = originUri.Host;
        if (hostSuffix.StartsWith('.'))
            return host.EndsWith(hostSuffix, StringComparison.OrdinalIgnoreCase);

        return string.Equals(host, hostSuffix, StringComparison.OrdinalIgnoreCase);
    }
}
