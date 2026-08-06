using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Maps vendor SDK status integers to <see cref="DeviceErrorCode"/>.
/// </summary>
/// <remarks>
/// Literal values match CFApi.h <c>STAT_*</c> (also used by Driver/Native).
/// Application never sees these integers — only <see cref="DeviceErrorCode"/>.
/// </remarks>
internal static class DeviceErrorMapper
{
    // CFApi.h / NativeConstants (internal to Sdk) — mirrored here for Infrastructure mapping only.
    private const int StatOk = 0;
    private const int StatPortHandleErr = unchecked((int)0xFFFFFF01);
    private const int StatPortOpenFailed = unchecked((int)0xFFFFFF02);
    private const int StatDllInnerFailed = unchecked((int)0xFFFFFF03);
    private const int StatCmdParamErr = unchecked((int)0xFFFFFF04);
    private const int StatCmdInnerErr = unchecked((int)0xFFFFFF06);
    private const int StatCmdInventoryStop = unchecked((int)0xFFFFFF07);
    private const int StatCmdTagNoResp = unchecked((int)0xFFFFFF08);
    private const int StatCmdAuthFail = unchecked((int)0xFFFFFF0B);
    private const int StatCmdPwdErr = unchecked((int)0xFFFFFF0C);
    private const int StatCmdRespFormatErr = unchecked((int)0xFFFFFF0F);
    private const int StatCmdCommTimeout = unchecked((int)0xFFFFFF12);
    private const int StatCmdCommWrFailed = unchecked((int)0xFFFFFF13);
    private const int StatCmdCommRdFailed = unchecked((int)0xFFFFFF14);
    private const int StatCmdNomoreData = unchecked((int)0xFFFFFF15);
    private const int StatDllUnconnect = unchecked((int)0xFFFFFF16);
    private const int StatDllDisconnect = unchecked((int)0xFFFFFF17);
    private const int StatIsoTagOprLimit = unchecked((int)0xFFFFFF52);
    private const int StatIsoTagMemLck = unchecked((int)0xFFFFFF54);

    public static DeviceErrorCode FromVendorStatus(int statusCode, bool success)
    {
        if (success || statusCode == StatOk)
            return DeviceErrorCode.None;

        return statusCode switch
        {
            StatPortHandleErr => DeviceErrorCode.ReaderNotConnected,
            StatDllUnconnect => DeviceErrorCode.ReaderNotConnected,
            StatPortOpenFailed => DeviceErrorCode.ReaderOpenFailed,
            StatDllInnerFailed => DeviceErrorCode.SdkUnavailable,
            StatCmdParamErr => DeviceErrorCode.InvalidParameter,
            StatCmdInnerErr => DeviceErrorCode.ReaderBusy,
            StatCmdInventoryStop => DeviceErrorCode.TagNotFound,
            StatCmdTagNoResp => DeviceErrorCode.TagNotFound,
            StatCmdNomoreData => DeviceErrorCode.TagNotFound,
            StatCmdAuthFail => DeviceErrorCode.InvalidPassword,
            StatCmdPwdErr => DeviceErrorCode.InvalidPassword,
            StatCmdCommTimeout => DeviceErrorCode.ReaderTimeout,
            StatCmdRespFormatErr => DeviceErrorCode.ReaderTimeout,
            StatCmdCommWrFailed => DeviceErrorCode.WriteFailed,
            StatCmdCommRdFailed => DeviceErrorCode.ReadFailed,
            StatIsoTagMemLck => DeviceErrorCode.TagAccessDenied,
            StatIsoTagOprLimit => DeviceErrorCode.TagAccessDenied,
            StatDllDisconnect => DeviceErrorCode.ReaderDisconnected,
            _ => DeviceErrorCode.Unknown,
        };
    }
}
