using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Maps <see cref="ICardConnection"/> to SDK <see cref="IUhfSdk.Connection"/>.
/// </summary>
/// <remarks>Not thread-safe. No retry, logging, or business rules.</remarks>
public sealed class CardConnectionAdapter : ICardConnection
{
    private readonly IUhfSdk _sdk;

    /// <summary>Creates an adapter over a shared SDK instance.</summary>
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
}
