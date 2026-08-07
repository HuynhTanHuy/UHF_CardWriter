using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk;
using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Maps <see cref="ICardReader"/> to SDK read (EPC bank defaults).
/// </summary>
/// <remarks>Not thread-safe. MemBank/wordPtr are Infrastructure concerns.</remarks>
public sealed class CardReaderAdapter : ICardReader
{
    private readonly IUhfSdk _sdk;

    /// <exception cref="ArgumentNullException"><paramref name="sdk"/> is null.</exception>
    public CardReaderAdapter(IUhfSdk sdk)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
    }

    /// <inheritdoc />
    public DeviceResult<CardReadResult> ReadEpc(
        byte[] accessPassword,
        byte wordCount,
        ushort responseTimeoutMs = DeviceConstants.DefaultReadResponseTimeoutMs) =>
        DeviceExceptionTranslator.Execute(() =>
            SdkMapping.ToDevice(
                _sdk.Reader.Read(
                    option: 0,
                    accessPassword,
                    MemBank.Epc,
                    SdkMapping.Gen2EpcWordPtr,
                    wordCount,
                    responseTimeoutMs),
                SdkMapping.ToCardReadResult));
}
