using System.Text;
using System.Text.Json;
using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;
using CareHR.UhfCardWriter.Application.Services;

namespace CareHR.UhfCardWriter.Infrastructure.Registration;

/// <summary>
/// Maps <see cref="ICardRegistrar"/> to CareHR <c>POST /api/rfid/cards</c>
/// (same contract as CareHR frontend create card).
/// </summary>
/// <remarks>No business rules — Application enforces verify-before-register. No SDK types.</remarks>
public sealed class HttpCardRegistrarAdapter : ICardRegistrar, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly CareHrCardApiOptions _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public HttpCardRegistrarAdapter(CareHrCardApiOptions options)
        : this(options, new HttpClient(), ownsHttp: true)
    {
    }

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
            return RegistrationResult.Fail(DeviceErrorCode.RegistrationFailed, "Thiếu Api.BaseUrl.");

        var token = (_options.BearerToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(token))
            return RegistrationResult.Fail(DeviceErrorCode.RegistrationFailed, "Thiếu Api.BearerToken.");

        var hospitalRaw = FirstNonEmpty(request.HospitalId, _options.DefaultHospitalId);
        if (string.IsNullOrWhiteSpace(hospitalRaw))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "Thiếu bệnh viện (hospitalId).");
        if (!Guid.TryParse(hospitalRaw, out var hospitalId))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "hospitalId không đúng định dạng GUID.");

        var typeId = (request.CardTypeId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(typeId))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "Thiếu loại thẻ.");
        if (!Guid.TryParse(typeId, out var typeGuid))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "Loại thẻ không đúng định dạng.");

        var batch = (request.BatchCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(batch))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "Thiếu lô thẻ.");

        var cardNumber = CardNumberBuilder.ToCardNumberFromEpcBytes(request.Identity.Epc);
        if (string.IsNullOrWhiteSpace(cardNumber))
            return RegistrationResult.Fail(DeviceErrorCode.InvalidParameter, "Thiếu mã thẻ.");

        var path = string.IsNullOrWhiteSpace(_options.CreateRfidCardPath)
            ? "/api/rfid/cards"
            : _options.CreateRfidCardPath.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;

        // Matches CareHR frontend:
        // { hospitalId, rfidCardNumber, rfidCardTypeId, rfidCardBatchCode, status, isActive }
        var body = new CreateRfidCardBody
        {
            HospitalId = hospitalId,
            RfidCardNumber = cardNumber,
            RfidCardTypeId = typeGuid,
            RfidCardBatchCode = batch,
            Status = _options.DefaultStatus,
            IsActive = _options.DefaultIsActive,
        };

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, baseUrl + path);
            message.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            message.Headers.TryAddWithoutValidation("Authorization", NormalizeBearer(token));
            message.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = _http.SendAsync(message).GetAwaiter().GetResult();
            var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var detail = string.IsNullOrWhiteSpace(text) ? response.ReasonPhrase ?? string.Empty : text;
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
                return RegistrationResult.Ok(string.IsNullOrWhiteSpace(detail) ? "Registered" : Truncate(detail, 240));

            return RegistrationResult.Fail(
                DeviceErrorCode.RegistrationFailed,
                ToUserFacingHttpError(statusCode, detail));
        }
        catch (Exception ex)
        {
            return RegistrationResult.Fail(
                DeviceErrorCode.RegistrationFailed,
                ToUserFacingException(ex));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }

    private static string ToUserFacingHttpError(int statusCode, string body)
    {
        if (!string.IsNullOrEmpty(body)
            && body.Contains("đã tồn tại", StringComparison.OrdinalIgnoreCase))
            return "Card number already exists in this hospital.";

        return statusCode switch
        {
            401 => "API authentication failed. Update Api.BearerToken.",
            403 => "Not authorized to create RFID cards.",
            404 => "API endpoint not found. Check Api.BaseUrl / CreateRfidCardPath.",
            409 => "Conflict while registering the card.",
            >= 500 => "CareHR server error. Retry later or contact IT.",
            _ => $"Registration failed (HTTP {statusCode}).",
        };
    }

    private static string ToUserFacingException(Exception ex) =>
        ex switch
        {
            HttpRequestException => "Cannot reach the CareHR API. Check network and Api.BaseUrl.",
            TaskCanceledException => "API request timed out.",
            _ => "Registration failed due to a network or client error.",
        };

    private static string Truncate(string text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max)
            return text;
        return text[..(max - 3)] + "...";
    }

    private static string FirstNonEmpty(string? a, string? b)
    {
        if (!string.IsNullOrWhiteSpace(a))
            return a.Trim();
        if (!string.IsNullOrWhiteSpace(b))
            return b.Trim();
        return string.Empty;
    }

    private static string NormalizeBearer(string token)
    {
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return token;
        return "Bearer " + token;
    }

    private sealed class CreateRfidCardBody
    {
        public Guid HospitalId { get; set; }
        public string RfidCardNumber { get; set; } = string.Empty;
        public Guid RfidCardTypeId { get; set; }
        public string RfidCardBatchCode { get; set; } = string.Empty;
        public int Status { get; set; }
        public bool IsActive { get; set; }
    }
}
