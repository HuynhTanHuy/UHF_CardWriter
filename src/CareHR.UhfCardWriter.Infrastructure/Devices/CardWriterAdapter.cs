using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk;
using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Maps <see cref="ICardWriter"/> to SDK write (EPC bank defaults).
/// </summary>
/// <remarks>Not thread-safe. MemBank/wordPtr are Infrastructure concerns.</remarks>
public sealed class CardWriterAdapter : ICardWriter
{
    private readonly IUhfSdk _sdk;

    /// <exception cref="ArgumentNullException"><paramref name="sdk"/> is null.</exception>
    public CardWriterAdapter(IUhfSdk sdk)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
    }

    /// <inheritdoc />
    public DeviceResult<CardWriteResult> WriteEpc(
        byte[] accessPassword,
        byte[] epcPayload,
        ushort responseTimeoutMs = DeviceConstants.DefaultWriteResponseTimeoutMs) =>
        DeviceExceptionTranslator.Execute(() =>
            SdkMapping.ToDevice(
                _sdk.Writer.Write(
                    option: 0,
                    accessPassword,
                    MemBank.Epc,
                    SdkMapping.Gen2EpcWordPtr,
                    epcPayload,
                    responseTimeoutMs),
                SdkMapping.ToCardWriteResult));
}
