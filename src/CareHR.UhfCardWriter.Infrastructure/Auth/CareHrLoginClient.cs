using System.Text;
using System.Text.Json;
using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Models;
using CareHR.UhfCardWriter.Infrastructure.Diagnostics;
using CareHR.UhfCardWriter.Infrastructure.Registration;

namespace CareHR.UhfCardWriter.Infrastructure.Auth;

/// <summary>
/// Calls CareHR <c>POST /api/auth/login</c> and returns <c>data.token</c>.
/// Does not log password, JWT, or Authorization headers.
/// </summary>
public sealed class CareHrLoginClient : ICareHrLoginClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly CareHrCardApiOptions _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public CareHrLoginClient(CareHrCardApiOptions options)
        : this(options, new HttpClient(), ownsHttp: true)
    {
    }

    public CareHrLoginClient(CareHrCardApiOptions options, HttpClient httpClient)
        : this(options, httpClient, ownsHttp: false)
    {
    }

    private CareHrLoginClient(CareHrCardApiOptions options, HttpClient httpClient, bool ownsHttp)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttp = ownsHttp;
    }

    public async Task<CareHrLoginResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(user))
            return CareHrLoginResult.Fail("Vui lòng nhập tên đăng nhập.");
        if (string.IsNullOrEmpty(password))
            return CareHrLoginResult.Fail("Vui lòng nhập mật khẩu.");

        var baseUrl = (_options.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return CareHrLoginResult.Fail("Thiếu Api.BaseUrl.");

        var url = baseUrl + "/api/auth/login";
        AuthHttpDiag.Log($"[HTTP] Operation=Login Method=POST Url={url} HasCredentials=true");

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, url);
            message.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            message.Content = new StringContent(
                JsonSerializer.Serialize(new LoginBody { Username = user, Password = password }, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.SendAsync(message, cancellationToken).ConfigureAwait(false);
            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;
            AuthHttpDiag.LogHttpResult("Login", statusCode, response.ReasonPhrase, text);

            if (statusCode == 401)
                return CareHrLoginResult.Fail("Tên đăng nhập hoặc mật khẩu không đúng.");

            if (statusCode == 403)
                return CareHrLoginResult.Fail(ExtractMessage(text) ?? "Bạn không có quyền truy cập.");

            if (statusCode >= 500)
                return CareHrLoginResult.Fail($"CareHR server error (HTTP {statusCode}). Thử lại sau hoặc liên hệ IT.");

            if (!TryParseLoginResponse(text, out var token, out var apiMessage, out var successFlag))
            {
                if (statusCode is >= 400 and < 500)
                    return CareHrLoginResult.Fail("Tên đăng nhập hoặc mật khẩu không đúng.");
                return CareHrLoginResult.Fail($"Đăng nhập thất bại (HTTP {statusCode}).");
            }

            if (!successFlag || string.IsNullOrWhiteSpace(token))
            {
                return CareHrLoginResult.Fail(
                    string.IsNullOrWhiteSpace(apiMessage)
                        ? "Tên đăng nhập hoặc mật khẩu không đúng."
                        : apiMessage);
            }

            return CareHrLoginResult.Ok(token.Trim());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CareHrLoginResult.Fail("Kết nối CareHR API hết thời gian chờ.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return CareHrLoginResult.Fail("Không kết nối được CareHR API. Kiểm tra mạng và Api.BaseUrl.");
        }
        catch (Exception ex)
        {
            AuthHttpDiag.Log($"[HTTP] Result Operation=Login Exception={ex.GetType().Name}");
            return CareHrLoginResult.Fail("Đăng nhập thất bại do lỗi kết nối.");
        }
    }

    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }

    private static bool TryParseLoginResponse(
        string? text,
        out string? token,
        out string? message,
        out bool success)
    {
        token = null;
        message = null;
        success = false;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (TryGetPropertyIgnoreCase(root, "success", out var successEl))
            {
                success = successEl.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => false,
                };
            }

            message = ExtractMessage(root);

            if (TryGetPropertyIgnoreCase(root, "data", out var dataEl) &&
                dataEl.ValueKind == JsonValueKind.Object &&
                TryGetPropertyIgnoreCase(dataEl, "token", out var tokenEl) &&
                tokenEl.ValueKind == JsonValueKind.String)
            {
                token = tokenEl.GetString();
                if (!string.IsNullOrWhiteSpace(token))
                    success = true;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ExtractMessage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(text);
            return ExtractMessage(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractMessage(JsonElement root)
    {
        if (TryGetPropertyIgnoreCase(root, "message", out var msgEl) &&
            msgEl.ValueKind == JsonValueKind.String)
        {
            var m = msgEl.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(m))
                return m;
        }

        if (TryGetPropertyIgnoreCase(root, "errors", out var errorsEl) &&
            errorsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in errorsEl.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                        return s;
                }
            }
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private sealed class LoginBody
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
