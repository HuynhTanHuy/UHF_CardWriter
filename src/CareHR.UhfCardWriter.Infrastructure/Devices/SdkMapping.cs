using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk.Models;
using AppCardIdentity = CareHR.UhfCardWriter.Application.Devices.CardIdentity;
using AppCardInformation = CareHR.UhfCardWriter.Application.Devices.CardInformation;
using AppCardReadResult = CareHR.UhfCardWriter.Application.Devices.CardReadResult;
using AppCardWriteResult = CareHR.UhfCardWriter.Application.Devices.CardWriteResult;
using SdkTagAccessResponse = CareHR.UhfCardWriter.Sdk.Models.TagAccessResponse;
using SdkTagIdentity = CareHR.UhfCardWriter.Sdk.Models.TagIdentity;
using SdkTagReadData = CareHR.UhfCardWriter.Sdk.Models.TagReadData;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Maps SDK Wrapper results/models to Application card-domain types.
/// </summary>
internal static class SdkMapping
{
    /// <summary>Gen2 EPC/UII bank — Infrastructure-only; not exposed to Application.</summary>
    public const byte Gen2EpcMemBank = (byte)MemBank.Epc;

    /// <summary>Standard word pointer for EPC payload (PC words skipped) — Desk Reader convention.</summary>
    public const ushort Gen2EpcWordPtr = 2;

    public static DeviceResult ToDevice(SdkResult result)
    {
        var error = DeviceErrorMapper.FromVendorStatus(result.StatusCode, result.Success);
        return new DeviceResult(error, result.Success, result.Message);
    }

    public static DeviceResult<T> ToDevice<T>(SdkResult<T> result)
    {
        var error = DeviceErrorMapper.FromVendorStatus(result.StatusCode, result.Success);
        return new DeviceResult<T>(error, result.Success, result.Message, result.Value);
    }

    public static DeviceResult<TOut> ToDevice<TIn, TOut>(SdkResult<TIn> result, Func<TIn, TOut> map)
    {
        var error = DeviceErrorMapper.FromVendorStatus(result.StatusCode, result.Success);
        if (!result.Success || result.Value is null)
            return new DeviceResult<TOut>(error, false, result.Message, default);

        return new DeviceResult<TOut>(error, true, result.Message, map(result.Value));
    }

    public static AppCardInformation ToCardInformation(SdkTagIdentity source)
    {
        var identity = new AppCardIdentity(Copy(source.Epc));
        return new AppCardInformation(
            identity,
            source.NO,
            source.RssiTenthsDbm,
            source.Antenna,
            source.Channel,
            Copy(source.Crc),
            Copy(source.Pc));
    }

    public static AppCardWriteResult ToCardWriteResult(SdkTagAccessResponse source) =>
        new(
            source.TagStatus,
            source.Antenna,
            Copy(source.Crc),
            Copy(source.Pc),
            Copy(source.Code));

    public static AppCardReadResult ToCardReadResult(SdkTagReadData source) =>
        new(
            source.Response.TagStatus,
            source.Response.Antenna,
            Copy(source.Response.Crc),
            Copy(source.Response.Pc),
            Copy(source.Response.Code),
            source.WordCount,
            Copy(source.Data));

    private static byte[] Copy(byte[]? source)
    {
        if (source is null || source.Length == 0)
            return Array.Empty<byte>();
        var copy = new byte[source.Length];
        Buffer.BlockCopy(source, 0, copy, 0, source.Length);
        return copy;
    }
}
