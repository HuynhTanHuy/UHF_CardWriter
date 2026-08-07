using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.DependencyInjection;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;
using CareHR.UhfCardWriter.Application.Services;
using CareHR.UhfCardWriter.Infrastructure.DependencyInjection;
using CareHR.UhfCardWriter.Infrastructure.Registration;
using CareHR.UhfCardWriter.Sdk;
using Microsoft.Extensions.DependencyInjection;

// Phase 6D — Runtime composition resolve + mocked end-to-end write job (no hardware / no live API).

var failures = 0;

Console.WriteLine("=== Phase 6D Smoke: DI resolve (real adapters) ===");
failures += RunResolveSmoke();

Console.WriteLine();
Console.WriteLine("=== Phase 6D Smoke: Workflow with mocked ports ===");
failures += RunWorkflowSmoke();

Console.WriteLine();
Console.WriteLine(failures == 0 ? "PHASE 6D SMOKE: PASS" : $"PHASE 6D SMOKE: FAIL ({failures})");
return failures == 0 ? 0 : 1;

static int RunResolveSmoke()
{
    var failed = 0;
    using var sp = new ServiceCollection()
        .AddCareHrCardWriter(o =>
        {
            o.BaseUrl = "https://example.invalid";
            o.BearerToken = "test-token";
        })
        .BuildServiceProvider();

    failed += AssertResolve<ICardConnection>(sp);
    failed += AssertResolve<ICardScanner>(sp);
    failed += AssertResolve<ICardWriter>(sp);
    failed += AssertResolve<ICardReader>(sp);
    failed += AssertResolve<ICardSecurity>(sp);
    failed += AssertResolve<ICardRegistrar>(sp);
    failed += AssertResolve<IUhfSdk>(sp);
    failed += AssertResolve<CareHrCardApiOptions>(sp);
    failed += AssertResolve<CardConnectionService>(sp);
    failed += AssertResolve<CardScanningService>(sp);
    failed += AssertResolve<CardReadingService>(sp);
    failed += AssertResolve<CardWritingService>(sp);
    failed += AssertResolve<CardVerificationService>(sp);
    failed += AssertResolve<CardRegistrationService>(sp);
    failed += AssertResolve<CardWriteOrchestrator>(sp);

    return failed;
}

static int AssertResolve<T>(ServiceProvider sp) where T : notnull
{
    try
    {
        var service = sp.GetRequiredService<T>();
        Console.WriteLine($"OK  {typeof(T).Name} -> {service.GetType().Name}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL {typeof(T).Name}: {ex.Message}");
        return 1;
    }
}

static int RunWorkflowSmoke()
{
    var intended = new byte[] { 0xE2, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA };
    var password = new byte[] { 0, 0, 0, 0 };
    var current = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };

    var connection = new FakeConnection { IsOpen = true };
    var scanner = new FakeScanner(current);
    var writer = new FakeWriter();
    var reader = new FakeReader(() => writer.LastWrittenEpc ?? current);
    var registrar = new FakeRegistrar();

    var services = new ServiceCollection();
    services.AddSingleton<ICardConnection>(connection);
    services.AddSingleton<ICardScanner>(scanner);
    services.AddSingleton<ICardWriter>(writer);
    services.AddSingleton<ICardReader>(reader);
    services.AddSingleton<ICardRegistrar>(registrar);
    services.AddApplicationServices();

    using var sp = services.BuildServiceProvider();
    var orch = sp.GetRequiredService<CardWriteOrchestrator>();

    var result = orch.RunWriteCardJob(new CardWriteJobRequest(
        new CardIdentity(intended),
        password,
        hospitalId: Guid.NewGuid().ToString(),
        cardTypeId: Guid.NewGuid().ToString(),
        batchCode: "BATCH-6D",
        scanTimeoutMs: 200));

    if (!result.Success || result.Stage != CardWriteJobStage.Completed)
    {
        Console.WriteLine($"FAIL workflow: Success={result.Success} Stage={result.Stage} {result.ErrorCode} {result.Message}");
        return 1;
    }

    if (!writer.WriteCalled || !registrar.RegisterCalled)
    {
        Console.WriteLine("FAIL workflow: write/register not called");
        return 1;
    }

    Console.WriteLine($"OK  RunWriteCardJob -> {result.Stage}: {result.Message}");
    return 0;
}

sealed class FakeConnection : ICardConnection
{
    public bool IsOpen { get; set; }
    public DeviceResult OpenSerial(string comPort, int baudRate) { IsOpen = true; return DeviceResult.Ok(); }
    public DeviceResult OpenHid(ushort index) { IsOpen = true; return DeviceResult.Ok(); }
    public DeviceResult OpenNet(string ip, ushort port, int timeoutMs) { IsOpen = true; return DeviceResult.Ok(); }
    public DeviceResult Close() { IsOpen = false; return DeviceResult.Ok(); }
    public DeviceResult<int> GetUsbDeviceCount() => DeviceResult<int>.Ok(0);
    public DeviceResult<string> GetUsbDeviceInfo(ushort index, int capacity = DeviceConstants.DefaultUsbInfoCapacity) =>
        DeviceResult<string>.Ok(string.Empty);
}

sealed class FakeScanner : ICardScanner
{
    private readonly byte[] _epc;
    public FakeScanner(byte[] epc) => _epc = epc;
    public DeviceResult StartScan(byte invCount = 0, uint invParam = 0) => DeviceResult.Ok();
    public DeviceResult StopScan(ushort timeoutMs = DeviceConstants.DefaultInventoryStopTimeoutMs) => DeviceResult.Ok();
    public DeviceResult<CardInformation> TryGetCard(ushort timeoutMs) =>
        DeviceResult<CardInformation>.Ok(new CardInformation(
            new CardIdentity(_epc), 1, -400, 1, 1, Array.Empty<byte>(), Array.Empty<byte>()));
    public DeviceResult SelectByIdentity(CardIdentity identity) => DeviceResult.Ok();
}

sealed class FakeWriter : ICardWriter
{
    public bool WriteCalled { get; private set; }
    public byte[]? LastWrittenEpc { get; private set; }
    public DeviceResult<CardWriteResult> WriteEpc(byte[] accessPassword, byte[] epcPayload, ushort responseTimeoutMs = DeviceConstants.DefaultWriteResponseTimeoutMs)
    {
        WriteCalled = true;
        LastWrittenEpc = (byte[])epcPayload.Clone();
        return DeviceResult<CardWriteResult>.Ok(new CardWriteResult(0, 1, Array.Empty<byte>(), Array.Empty<byte>(), epcPayload));
    }
}

sealed class FakeReader : ICardReader
{
    private readonly Func<byte[]> _epc;
    public FakeReader(Func<byte[]> epc) => _epc = epc;
    public DeviceResult<CardReadResult> ReadEpc(byte[] accessPassword, byte wordCount, ushort responseTimeoutMs = DeviceConstants.DefaultReadResponseTimeoutMs)
    {
        var data = _epc();
        return DeviceResult<CardReadResult>.Ok(new CardReadResult(0, 1, Array.Empty<byte>(), Array.Empty<byte>(), data, wordCount, data));
    }
}

sealed class FakeRegistrar : ICardRegistrar
{
    public bool RegisterCalled { get; private set; }
    public RegistrationResult Register(RegistrationRequest request)
    {
        RegisterCalled = true;
        return RegistrationResult.Ok("fake-registered");
    }
}
