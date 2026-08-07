using System.Net;
using System.Text;
using System.Text.Json;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;
using CareHR.UhfCardWriter.Infrastructure.Registration;

// Phase 8 — CareHR POST /api/rfid/cards wire-compatibility smoke (captured request; no live Backend required).

var failures = 0;
var hospitalId = Guid.Parse("83340a8d-ca2c-4fd0-a6dc-367e28505752");
var typeId = Guid.Parse("4f36704f-c6ff-4d4a-a23b-48778bba7718");

Console.WriteLine("=== Phase 8 Compatibility Smoke: POST /api/rfid/cards ===");

failures += AssertRequestShape();
failures += AssertAuthHeader();
failures += AssertErrorMapping();
failures += AssertPreflightMessages();

Console.WriteLine();
Console.WriteLine(failures == 0
    ? "PHASE 8 COMPATIBILITY SMOKE: PASS (CareHR /api/rfid/cards)"
    : $"PHASE 8 COMPATIBILITY SMOKE: FAIL ({failures})");
return failures == 0 ? 0 : 1;

int AssertRequestShape()
{
    var epcBytes = Encoding.ASCII.GetBytes("tesh");
    var identity = new CardIdentity(epcBytes);

    var capture = new CapturingHandler(_ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json"),
        });

    using var http = new HttpClient(capture);
    var adapter = new HttpCardRegistrarAdapter(
        new CareHrCardApiOptions
        {
            BaseUrl = "https://carehr02-mgl-api.2bsolu.com",
            BearerToken = "test-token",
            CreateRfidCardPath = "/api/rfid/cards",
            DefaultStatus = 4,
            DefaultIsActive = true,
        },
        http);

    var result = adapter.Register(new RegistrationRequest(
        identity,
        hospitalId.ToString(),
        typeId.ToString(),
        "BATCH-001",
        isVerified: true));

    var failed = 0;
    failed += Expect(result.Success, "Success on 200");
    failed += Expect(capture.LastRequest is not null, "Request captured");
    if (capture.LastRequest is null)
        return failed;

    var req = capture.LastRequest;
    failed += Expect(req.Method == HttpMethod.Post, "POST");
    failed += Expect(
        req.RequestUri!.AbsoluteUri == "https://carehr02-mgl-api.2bsolu.com/api/rfid/cards",
        "URI == /api/rfid/cards");

    var json = capture.LastBody ?? string.Empty;
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;
    failed += Expect(root.GetProperty("hospitalId").GetGuid() == hospitalId, "hospitalId");
    failed += Expect(root.GetProperty("rfidCardNumber").GetString() == "tesh", "rfidCardNumber ASCII");
    failed += Expect(root.GetProperty("rfidCardTypeId").GetGuid() == typeId, "rfidCardTypeId");
    failed += Expect(root.GetProperty("rfidCardBatchCode").GetString() == "BATCH-001", "rfidCardBatchCode");
    failed += Expect(root.GetProperty("status").GetInt32() == 4, "status=4 Stock");
    failed += Expect(root.GetProperty("isActive").GetBoolean(), "isActive");

    failed += Expect(json.Contains("\"hospitalId\"", StringComparison.Ordinal), "camelCase hospitalId");
    failed += Expect(json.Contains("\"rfidCardNumber\"", StringComparison.Ordinal), "camelCase rfidCardNumber");

    Console.WriteLine(failed == 0 ? "OK  Request shape / body" : "FAIL Request shape");
    return failed;
}

int AssertAuthHeader()
{
    var capture = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
    using var http = new HttpClient(capture);
    var adapter = new HttpCardRegistrarAdapter(
        new CareHrCardApiOptions
        {
            BaseUrl = "https://carehr02-mgl-api.2bsolu.com",
            BearerToken = "raw-jwt-without-prefix",
            DefaultHospitalId = hospitalId.ToString(),
        },
        http);

    _ = adapter.Register(new RegistrationRequest(
        new CardIdentity(Encoding.ASCII.GetBytes("x")),
        hospitalId.ToString(),
        typeId.ToString(),
        "B1",
        true));

    var auth = string.Empty;
    if (capture.LastRequest is not null &&
        capture.LastRequest.Headers.TryGetValues("Authorization", out var values))
        auth = values.FirstOrDefault() ?? string.Empty;

    var failed = Expect(auth == "Bearer raw-jwt-without-prefix", "Authorization Bearer normalize");
    Console.WriteLine(failed == 0 ? "OK  Authorization header" : $"FAIL Authorization: '{auth}'");
    return failed;
}

int AssertErrorMapping()
{
    var failed = 0;
    failed += MapStatus(HttpStatusCode.Unauthorized, expectSuccess: false);
    failed += MapStatus(HttpStatusCode.Forbidden, expectSuccess: false);
    failed += MapStatus(HttpStatusCode.NotFound, expectSuccess: false);
    failed += MapStatus(HttpStatusCode.Conflict, expectSuccess: false);
    failed += MapStatus(HttpStatusCode.InternalServerError, expectSuccess: false);
    failed += MapStatus(HttpStatusCode.OK, expectSuccess: true);

    var boom = new CapturingHandler(_ => throw new HttpRequestException("connection refused"));
    using var http = new HttpClient(boom);
    var adapter = new HttpCardRegistrarAdapter(
        new CareHrCardApiOptions { BaseUrl = "https://x", BearerToken = "t" },
        http);
    var net = adapter.Register(new RegistrationRequest(
        new CardIdentity(Encoding.ASCII.GetBytes("a")),
        hospitalId.ToString(),
        typeId.ToString(),
        "B",
        true));
    failed += Expect(!net.Success && net.Message.Contains("CareHR API", StringComparison.OrdinalIgnoreCase),
        "Network exception → friendly Fail");

    Console.WriteLine(failed == 0 ? "OK  Error mapping" : "FAIL Error mapping");
    return failed;
}

int MapStatus(HttpStatusCode code, bool expectSuccess)
{
    var capture = new CapturingHandler(_ => new HttpResponseMessage(code)
    {
        Content = new StringContent("{\"errors\":[\"Mã thẻ đã tồn tại trong bệnh viện.\"]}", Encoding.UTF8, "application/json"),
    });
    using var http = new HttpClient(capture);
    var adapter = new HttpCardRegistrarAdapter(
        new CareHrCardApiOptions { BaseUrl = "https://x", BearerToken = "t" },
        http);
    var result = adapter.Register(new RegistrationRequest(
        new CardIdentity(Encoding.ASCII.GetBytes("a")),
        hospitalId.ToString(),
        typeId.ToString(),
        "B",
        true));
    return Expect(result.Success == expectSuccess, $"Status {(int)code} success={expectSuccess}");
}

int AssertPreflightMessages()
{
    var failed = 0;
    using var http = new HttpClient(new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

    var noUrl = new HttpCardRegistrarAdapter(new CareHrCardApiOptions { BearerToken = "t" }, http);
    var r1 = noUrl.Register(new RegistrationRequest(
        new CardIdentity(Encoding.ASCII.GetBytes("a")), hospitalId.ToString(), typeId.ToString(), "B", true));
    failed += Expect(!r1.Success && r1.Message.Contains("BaseUrl", StringComparison.Ordinal), "Missing BaseUrl");

    var noToken = new HttpCardRegistrarAdapter(new CareHrCardApiOptions { BaseUrl = "https://x" }, http);
    var r2 = noToken.Register(new RegistrationRequest(
        new CardIdentity(Encoding.ASCII.GetBytes("a")), hospitalId.ToString(), typeId.ToString(), "B", true));
    failed += Expect(!r2.Success && r2.Message.Contains("BearerToken", StringComparison.Ordinal), "Missing token");

    Console.WriteLine(failed == 0 ? "OK  Preflight messages" : "FAIL Preflight");
    return failed;
}

static int Expect(bool condition, string name)
{
    if (condition)
        return 0;
    Console.WriteLine($"  FAIL {name}");
    return 1;
}

sealed class CapturingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return _respond(request);
    }
}
