using System.Text;
using System.Text.Json;
using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Infrastructure.Registration;

/// <summary>
/// Maps <see cref="ICardRegistrar"/> to CareHR HTTP OData CreateRfidTag (CardWritter-compatible).
/// </summary>
/// <remarks>No business rules — Application enforces verify-before-register. No SDK types.</remarks>
public sealed class HttpCardRegistrarAdapter : ICardRegistrar, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
    };

    private readonly CareHrCardApiOptions _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    /// <summary>Creates an adapter with a dedicated <see cref="HttpClient"/>.</summary>
    public HttpCardRegistrarAdapter(CareHrCardApiOptions options)
        : this(options, new HttpClient(), ownsHttp: true)
    {
    }

    /// <summary>Creates an adapter using a provided <see cref="HttpClient"/>.</summary>
    public HttpCardRegistrarAdapter(CareHrCardApiOptions options, HttpClient httpClient)
        : this(options, httpClient, ownsHttp: false)
    {
    }

    private HttpCardRegistrarAdapter(CareHrCardApiOptions options, HttpClient httpClient, bool ownsHttp)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttp = ownsHttp;
    }

    /// <inheritdoc />
    public RegistrationResult Register(RegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var baseUrl = (_options.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return RegistrationResult.Fail(DeviceErrorCode.RegistrationFailed, "CareHR API base URL is not configured.");

        var token = (_options.BearerToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(token))
            return RegistrationResult.Fail(DeviceErrorCode.RegistrationFailed, "CareHR API bearer token is not configured.");

        var typeId = (request.CardTypeId ?? string.Empty).Trim();
        if (!Guid.TryParse(typeId, out var typeGuid))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "Card type id must be a GUID.");

        var batch = (request.BatchCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(batch))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "Batch code is required.");

        var epc = request.Identity.EpcHex;
        if (string.IsNullOrWhiteSpace(epc))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "EPC identity is empty.");

        var path = string.IsNullOrWhiteSpace(_options.CreateRfidTagPath)
            ? "/odata/rfid/RfidTags"
            : _options.CreateRfidTagPath.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;

        var body = new
        {
            EPCCode = epc,
            RfidTagTypeId = typeGuid,
            RfidTagBatchCode = batch,
        };

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, baseUrl + path);
            message.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            message.Headers.TryAddWithoutValidation("Authorization", NormalizeBearer(token));
            message.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

            using var response = _http.Send(message);
            var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var detail = string.IsNullOrWhiteSpace(text) ? response.ReasonPhrase ?? string.Empty : text;

            if (response.IsSuccessStatusCode)
                return RegistrationResult.Ok(string.IsNullOrWhiteSpace(detail) ? "Registered" : detail);

            return RegistrationResult.Fail(
                DeviceErrorCode.RegistrationFailed,
                $"HTTP {(int)response.StatusCode}: {detail}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return RegistrationResult.Fail(DeviceErrorCode.RegistrationFailed, ex.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }

    private static string NormalizeBearer(string token)
    {
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return token;
        return "Bearer " + token;
    }
}
