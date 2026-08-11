using System.Diagnostics;
using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Maps <see cref="ICardConnection"/> to SDK <see cref="IUhfSdk.Connection"/>.
/// </summary>
/// <remarks>
/// DevicePara Get/Set are serialized via <see cref="_deviceParaGate"/> (one native call at a time).
/// Open/Close/USB enum and inventory/write paths are not gated here.
/// </remarks>
public sealed class CardConnectionAdapter : ICardConnection
{
    /// <summary>Optional sink for DevicePara gate diagnostics (wired by App to AppLog).</summary>
    public static Action<string>? DeviceParaDiag { get; set; }

    /// <summary>Serializes GetDevicePara/SetDevicePara only — driver is not thread-safe.</summary>
    private readonly SemaphoreSlim _deviceParaGate = new(1, 1);

    private readonly IUhfSdk _sdk;

    /// <exception cref="ArgumentNullException"><paramref name="sdk"/> is null.</exception>
    public CardConnectionAdapter(IUhfSdk sdk)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
    }

    /// <inheritdoc />
    public bool IsOpen =>
        DeviceExceptionTranslator.Execute(() => _sdk.Connection.IsOpen);

    /// <inheritdoc />
    public DeviceResult OpenSerial(string comPort, int baudRate) =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.Connection.OpenSerial(comPort, baudRate)));

    /// <inheritdoc />
    public DeviceResult OpenHid(ushort index) =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.Connection.OpenHid(index)));

    /// <inheritdoc />
    public DeviceResult OpenNet(string ip, ushort port, int timeoutMs) =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.Connection.OpenNet(ip, port, timeoutMs)));

    /// <inheritdoc />
    public DeviceResult Close() =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.Connection.Close()));

    /// <inheritdoc />
    public DeviceResult<int> GetUsbDeviceCount() =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.Connection.GetUsbDeviceCount()));

    /// <inheritdoc />
    public DeviceResult<string> GetUsbDeviceInfo(ushort index, int capacity = DeviceConstants.DefaultUsbInfoCapacity) =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.Connection.GetUsbDeviceInfo(index, capacity)));

    /// <inheritdoc />
    public DeviceResult<DeviceParameters> GetDevicePara() =>
        WithDeviceParaGate(
            "Get",
            () => DeviceExceptionTranslator.Execute(() =>
                SdkMapping.ToDevice(_sdk.Connection.GetDevicePara(), SdkMapping.ToDeviceParameters)));

    /// <inheritdoc />
    public DeviceResult SetDevicePara(DeviceParameters para) =>
        WithDeviceParaGate(
            "Set",
            () => DeviceExceptionTranslator.Execute(() =>
                SdkMapping.ToDevice(_sdk.Connection.SetDevicePara(SdkMapping.ToSdkDeviceParameters(para)))));

    private T WithDeviceParaGate<T>(string operation, Func<T> action)
    {
        var threadId = Environment.CurrentManagedThreadId;
        Diag($"[DevicePara] WaitStart Operation={operation} ThreadId={threadId}");
        _deviceParaGate.Wait();
        Diag($"[DevicePara] Acquired Operation={operation} ThreadId={threadId}");
        try
        {
            return action();
        }
        finally
        {
            _deviceParaGate.Release();
            Diag($"[DevicePara] Released Operation={operation} ThreadId={threadId}");
        }
    }

    private static void Diag(string message)
    {
        Trace.WriteLine(message);
        DeviceParaDiag?.Invoke(message);
    }
}
