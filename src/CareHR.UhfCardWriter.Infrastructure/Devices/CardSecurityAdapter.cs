using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Maps <see cref="ICardSecurity"/> to SDK lock/kill APIs.
/// </summary>
/// <remarks>Not thread-safe.</remarks>
public sealed class CardSecurityAdapter : ICardSecurity
{
    private readonly IUhfSdk _sdk;

    /// <exception cref="ArgumentNullException"><paramref name="sdk"/> is null.</exception>
    public CardSecurityAdapter(IUhfSdk sdk)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
    }

    /// <inheritdoc />
    public DeviceResult Lock(byte[] accessPassword, byte area, byte action) =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.TagControl.Lock(accessPassword, area, action)));

    /// <inheritdoc />
    public DeviceResult Kill(byte[] accessPassword) =>
        DeviceExceptionTranslator.Execute(() => SdkMapping.ToDevice(_sdk.TagControl.Kill(accessPassword)));
}
