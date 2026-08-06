using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Maps <see cref="ICardScanner"/> to SDK inventory + select APIs.
/// </summary>
/// <remarks>Not thread-safe. No inventory poll loop.</remarks>
public sealed class CardScannerAdapter : ICardScanner
{
    private readonly IUhfSdk _sdk;

    /// <summary>Creates an adapter over a shared SDK instance.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="sdk"/> is null.</exception>
    public CardScannerAdapter(IUhfSdk sdk)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
    }

    /// <inheritdoc />
    public DeviceResult StartScan(byte invCount = 0, uint invParam = 0) =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.Inventory.Start(invCount, invParam)));

    /// <inheritdoc />
    public DeviceResult StopScan(ushort timeoutMs = DeviceConstants.DefaultInventoryStopTimeoutMs) =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.Inventory.Stop(timeoutMs)));

    /// <inheritdoc />
    public DeviceResult<CardInformation> TryGetCard(ushort timeoutMs) =>
        DeviceExceptionTranslator.Execute(() =>
            SdkMapping.ToDevice(_sdk.Inventory.GetCurrentTag(timeoutMs), SdkMapping.ToCardInformation));

    /// <inheritdoc />
    public DeviceResult SelectByIdentity(CardIdentity identity)
    {
        if (identity is null)
            throw new ArgumentNullException(nameof(identity));
        if (identity.Epc.Length == 0)
            throw new ArgumentException("Card identity EPC is empty.", nameof(identity));

        var maskBits = (byte)Math.Min(byte.MaxValue, identity.Epc.Length * 8);
        return DeviceExceptionTranslator.Execute(() =>
            SdkMapping.ToDevice(_sdk.TagControl.Select(maskPtr: 0, maskBits, identity.Epc)));
    }
}
