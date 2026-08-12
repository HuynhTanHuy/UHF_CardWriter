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
    public const string AuthRequiredMessage =
        "Chưa đăng nhập CareHR hoặc chưa cấp quyền cho ứng dụng ghi thẻ.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly CareHrCardApiOptions _options;
    private readonly IWriterAuthSession _authSession;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public HttpCardRegistrarAdapter(CareHrCardApiOptions options, IWriterAuthSession authSession)
        : this(options, authSession, new HttpClient(), ownsHttp: true)
    {
    }

    public HttpCardRegistrarAdapter(
        CareHrCardApiOptions options,
        IWriterAuthSession authSession,
        HttpClient httpClient)
        : this(options, authSession, httpClient, ownsHttp: false)
    {
    }

    private HttpCardRegistrarAdapter(
        CareHrCardApiOptions options,
        IWriterAuthSession authSession,
        HttpClient httpClient,
        bool ownsHttp)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
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

        if (!TryGetAuthToken(out var token))
            return RegistrationResult.Fail(DeviceErrorCode.RegistrationFailed, AuthRequiredMessage);

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
    public CardExistenceResult Exists(string hospitalId, string rfidCardNumber)
    {
        var number = (rfidCardNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(number))
            return CardExistenceResult.NotFound("Empty card number.");

        var baseUrl = (_options.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return CardExistenceResult.Failed("Thiếu Api.BaseUrl.");

        if (!TryGetAuthToken(out var token))
            return CardExistenceResult.Failed(AuthRequiredMessage);

        var hospitalRaw = FirstNonEmpty(hospitalId, _options.DefaultHospitalId);
        Guid? hospitalGuid = null;
        if (!string.IsNullOrWhiteSpace(hospitalRaw) && Guid.TryParse(hospitalRaw, out var hid))
            hospitalGuid = hid;

        var path = string.IsNullOrWhiteSpace(_options.CreateRfidCardPath)
            ? "/api/rfid/cards"
            : _options.CreateRfidCardPath.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;

        // Reuse CareHR list API: GET /api/rfid/cards?Search={number}&Page=1&PageSize=50
        // Server Search is Contains — client requires exact RFIDCardNumber match.
        var url =
            baseUrl + path +
            "?Search=" + Uri.EscapeDataString(number) +
            "&Page=1&PageSize=50";

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            message.Headers.TryAddWithoutValidation("Authorization", NormalizeBearer(token));

            using var response = _http.SendAsync(message).GetAwaiter().GetResult();
            var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                return CardExistenceResult.Failed(
                    statusCode switch
                    {
                        401 => "API authentication failed. Authorize Card Writer from CareHR Frontend again.",
                        403 => "Not authorized to query RFID cards.",
                        404 => "API endpoint not found. Check Api.BaseUrl / CreateRfidCardPath.",
                        >= 500 => "CareHR server error during card existence check.",
                        _ => $"Card existence check failed (HTTP {statusCode}).",
                    });
            }

            if (string.IsNullOrWhiteSpace(text))
                return CardExistenceResult.NotFound();

            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (!TryGetPropertyIgnoreCase(root, "data", out var dataEl) ||
                dataEl.ValueKind != JsonValueKind.Array)
            {
                // Some envelopes may omit data when empty.
                return CardExistenceResult.NotFound();
            }

            foreach (var item in dataEl.EnumerateArray())
            {
                if (!TryGetPropertyIgnoreCase(item, "rfidCardNumber", out var numEl))
                    continue;

                var foundNumber = numEl.GetString()?.Trim() ?? string.Empty;
                if (!string.Equals(foundNumber, number, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (hospitalGuid.HasValue &&
                    TryGetPropertyIgnoreCase(item, "hospitalId", out var hospEl) &&
                    hospEl.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(hospEl.GetString(), out var itemHospital) &&
                    itemHospital != hospitalGuid.Value)
                {
                    continue;
                }

                return CardExistenceResult.Found(
                    $"Thẻ RFID {foundNumber} đã được đăng ký.");
            }

            return CardExistenceResult.NotFound();
        }
        catch (JsonException ex)
        {
            return CardExistenceResult.Failed("Invalid existence-check response: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CardExistenceResult.Failed(ToUserFacingException(ex));
        }
    }

    /// <inheritdoc />
    public NextSerialResult GetNextSerial(string hospitalId, string numberPrefix, int serialWidth)
    {
        var prefix = (numberPrefix ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(prefix))
            return NextSerialResult.Fail("Thiếu prefix HospitalNumber+Batch.");

        if (serialWidth <= 0)
            serialWidth = 5;

        var maxSerialExclusive = (int)Math.Pow(10, serialWidth);
        var expectedLength = prefix.Length + serialWidth;

        var baseUrl = (_options.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return NextSerialResult.Fail("Thiếu Api.BaseUrl.");

        if (!TryGetAuthToken(out var token))
            return NextSerialResult.Fail(AuthRequiredMessage);

        var hospitalRaw = FirstNonEmpty(hospitalId, _options.DefaultHospitalId);
        Guid? hospitalGuid = null;
        if (!string.IsNullOrWhiteSpace(hospitalRaw) && Guid.TryParse(hospitalRaw, out var hid))
            hospitalGuid = hid;

        var path = string.IsNullOrWhiteSpace(_options.CreateRfidCardPath)
            ? "/api/rfid/cards"
            : _options.CreateRfidCardPath.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;

        try
        {
            var maxSerial = 0;
            var page = 1;
            const int pageSize = 100;
            const int maxPages = 50;

            while (page <= maxPages)
            {
                var url =
                    baseUrl + path +
                    "?Search=" + Uri.EscapeDataString(prefix) +
                    "&Page=" + page.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    "&PageSize=" + pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);

                using var message = new HttpRequestMessage(HttpMethod.Get, url);
                message.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
                message.Headers.TryAddWithoutValidation("Authorization", NormalizeBearer(token));

                using var response = _http.SendAsync(message).GetAwaiter().GetResult();
                var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var statusCode = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    return NextSerialResult.Fail(
                        statusCode switch
                        {
                            401 => "API authentication failed. Authorize Card Writer from CareHR Frontend again.",
                            403 => "Not authorized to query RFID cards.",
                            404 => "API endpoint not found. Check Api.BaseUrl / CreateRfidCardPath.",
                            >= 500 => "CareHR server error while resolving next serial.",
                            _ => $"Next-serial query failed (HTTP {statusCode}).",
                        });
                }

                if (string.IsNullOrWhiteSpace(text))
                    break;

                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (!TryGetPropertyIgnoreCase(root, "data", out var dataEl) ||
                    dataEl.ValueKind != JsonValueKind.Array)
                {
                    break;
                }

                var countOnPage = 0;
                foreach (var item in dataEl.EnumerateArray())
                {
                    countOnPage++;
                    if (!TryGetPropertyIgnoreCase(item, "rfidCardNumber", out var numEl))
                        continue;

                    var foundNumber = numEl.GetString()?.Trim() ?? string.Empty;
                    if (foundNumber.Length != expectedLength)
                        continue;
                    if (!foundNumber.StartsWith(prefix, StringComparison.Ordinal))
                        continue;

                    if (hospitalGuid.HasValue &&
                        TryGetPropertyIgnoreCase(item, "hospitalId", out var hospEl) &&
                        hospEl.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(hospEl.GetString(), out var itemHospital) &&
                        itemHospital != hospitalGuid.Value)
                    {
                        continue;
                    }

                    var serialPart = foundNumber.AsSpan(prefix.Length);
                    var allDigits = true;
                    for (var i = 0; i < serialPart.Length; i++)
                    {
                        if (!char.IsDigit(serialPart[i]))
                        {
                            allDigits = false;
                            break;
                        }
                    }

                    if (!allDigits)
                        continue;

                    if (!int.TryParse(
                            serialPart,
                            System.Globalization.NumberStyles.None,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var serial))
                    {
                        continue;
                    }

                    if (serial > maxSerial)
                        maxSerial = serial;
                }

                var totalPages = 1;
                if (TryGetPropertyIgnoreCase(root, "totalPages", out var tp) &&
                    tp.TryGetInt32(out var tpVal) &&
                    tpVal > 0)
                {
                    totalPages = tpVal;
                }
                else if (TryGetPropertyIgnoreCase(root, "totalCount", out var tc) &&
                         tc.TryGetInt32(out var totalCount) &&
                         totalCount > 0)
                {
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                }

                if (countOnPage == 0 || page >= totalPages)
                    break;

                page++;
            }

            var next = maxSerial + 1;
            if (next < 1)
                next = 1;

            if (next >= maxSerialExclusive)
            {
                return NextSerialResult.Fail(
                    $"Serial vượt quá {serialWidth} chữ số (max {maxSerialExclusive - 1}) cho prefix {prefix}.");
            }

            return NextSerialResult.Ok(
                next,
                maxSerial == 0
                    ? $"No cards for prefix {prefix}; next serial = 1."
                    : $"Max serial for {prefix} = {maxSerial}; next = {next}.");
        }
        catch (JsonException ex)
        {
            return NextSerialResult.Fail("Invalid next-serial response: " + ex.Message);
        }
        catch (Exception ex)
        {
            return NextSerialResult.Fail(ToUserFacingException(ex));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
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

    private static string ToUserFacingHttpError(int statusCode, string body)
    {
        if (!string.IsNullOrEmpty(body)
            && body.Contains("đã tồn tại", StringComparison.OrdinalIgnoreCase))
            return "Card number already exists in this hospital.";

        return statusCode switch
        {
            401 => "API authentication failed. Authorize Card Writer from CareHR Frontend again.",
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

    private bool TryGetAuthToken(out string token)
    {
        if (_authSession.TryGetToken(out token) && !string.IsNullOrWhiteSpace(token))
        {
            token = token.Trim();
            return true;
        }

        token = string.Empty;
        return false;
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
