namespace CareHR.UhfCardWriter.Sdk.Native;

/// <summary>
/// Constants from CFApi.h — status codes, ISO command ids, timeouts.
/// Phase 2: mapping only; no business logic.
/// </summary>
internal static class NativeConstants
{
    public const string DllName = "UHFPrimeReader.dll";

    // --- Status / error (CFApi.h STAT_*) ---
    public const int StatOk = unchecked((int)0x00000000);
    public const int StatPortHandleErr = unchecked((int)0xFFFFFF01);
    public const int StatPortOpenFailed = unchecked((int)0xFFFFFF02);
    public const int StatDllInnerFailed = unchecked((int)0xFFFFFF03);
    public const int StatCmdParamErr = unchecked((int)0xFFFFFF04);
    public const int StatCmdSerialNumExit = unchecked((int)0xFFFFFF05);
    public const int StatCmdInnerErr = unchecked((int)0xFFFFFF06);
    public const int StatCmdInventoryStop = unchecked((int)0xFFFFFF07);
    public const int StatCmdTagNoResp = unchecked((int)0xFFFFFF08);
    public const int StatCmdDecodeTagDataFail = unchecked((int)0xFFFFFF09);
    public const int StatCmdCodeOverflow = unchecked((int)0xFFFFFF0A);
    public const int StatCmdAuthFail = unchecked((int)0xFFFFFF0B);
    public const int StatCmdPwdErr = unchecked((int)0xFFFFFF0C);
    public const int StatCmdSamNoResp = unchecked((int)0xFFFFFF0D);
    public const int StatCmdSamCmdFail = unchecked((int)0xFFFFFF0E);
    public const int StatCmdRespFormatErr = unchecked((int)0xFFFFFF0F);
    public const int StatCmdHasMoreData = unchecked((int)0xFFFFFF10);
    public const int StatCmdBufOverflow = unchecked((int)0xFFFFFF11);
    public const int StatCmdCommTimeout = unchecked((int)0xFFFFFF12);
    public const int StatCmdCommWrFailed = unchecked((int)0xFFFFFF13);
    public const int StatCmdCommRdFailed = unchecked((int)0xFFFFFF14);
    public const int StatCmdNomoreData = unchecked((int)0xFFFFFF15);
    public const int StatDllUnconnect = unchecked((int)0xFFFFFF16);
    public const int StatDllDisconnect = unchecked((int)0xFFFFFF17);
    public const int StatCmdRespCrcErr = unchecked((int)0xFFFFFF18);

    public const int StatIsoTagOtherErr = unchecked((int)0xFFFFFF50);
    public const int StatIsoTagNotSupport = unchecked((int)0xFFFFFF51);
    public const int StatIsoTagOprLimit = unchecked((int)0xFFFFFF52);
    public const int StatIsoTagMemOvf = unchecked((int)0xFFFFFF53);
    public const int StatIsoTagMemLck = unchecked((int)0xFFFFFF54);
    public const int StatIsoTagCryptoErr = unchecked((int)0xFFFFFF55);
    public const int StatIsoTagNotEncap = unchecked((int)0xFFFFFF56);
    public const int StatIsoTagRespOvf = unchecked((int)0xFFFFFF57);
    public const int StatIsoTagSecTimeout = unchecked((int)0xFFFFFF58);
    public const int StatIsoTagLowPower = unchecked((int)0xFFFFFF59);
    public const int StatIsoTagUnknwErr = unchecked((int)0xFFFFFF5A);

    // --- Timeouts (CFApi.h) ---
    public const int DefReadTimeout = 50;
    public const int DefWriteTimeout = 1000;
    public const int CommonTimeout = 2000;
    public const int SpecialTimeout = 300;
    public const int Timeout1500 = 1500;
    public const int Timeout2000 = 2000;
    public const int Timeout4000 = 4000;
    public const int Timeout5000 = 5000;
    public const int Timeout10000 = 10000;

    // --- ISO access command codes (for GetTagResp) ---
    public const ushort IsoInventoryContinue = 0x0001;
    public const ushort IsoInventoryStop = 0x0002;
    public const ushort IsoReadTag = 0x0003;
    public const ushort IsoWriteTag = 0x0004;
    public const ushort IsoLockTag = 0x0005;
    public const ushort IsoKillTag = 0x0006;
    public const ushort IsoSetSelectMask = 0x0007;

    // --- Reader response bytes (R_RES_*) ---
    public const byte RResOk = 0x00;
    public const byte RResParamErr = 0x01;
    public const byte RResOprErr = 0x02;
    public const byte RResInventEnd = 0x12;
    public const byte RResTagNoResp = 0x14;
    public const byte RResTagCrcErr = 0x15;
    public const byte RResAuthFailed = 0x16;
    public const byte RResTagPwdErr = 0x17;
    public const byte RResNomoreData = 0xFF;

    public const int InvalidHandleValue = -1;
}
