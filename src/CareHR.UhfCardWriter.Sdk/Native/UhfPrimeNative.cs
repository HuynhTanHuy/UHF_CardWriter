using System.Runtime.InteropServices;
using System.Text;

namespace CareHR.UhfCardWriter.Sdk.Native;

/// <summary>
/// P/Invoke surface for UHFPrimeReader.dll — Phase 2 only.
/// Signatures follow CFApi.h; CallingConvention/CharSet follow SDK sample C#.
/// No business logic. No high-level wrapper.
/// </summary>
internal static class UhfPrimeNative
{
    private const string Dll = NativeConstants.DllName;

    static UhfPrimeNative()
    {
        NativeLayout.ValidateOrThrow();
    }

    // ----------------- Connection -----------------

    /// <summary>CFApi.h: int OpenDevice(int64_t* hComm, char* pcCom, int iBaudRate);</summary>
    [DllImport(Dll, EntryPoint = "OpenDevice", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = false)]
    internal static extern int OpenDevice(
        out IntPtr hComm,
        [MarshalAs(UnmanagedType.LPStr)] string pcCom,
        int iBaudRate);

    /// <summary>
    /// CFApi.h: int OpenNetConnection(int64_t* hComm, char* strIP, unsigned short wPort, long timeoutMs);
    /// Windows MSVC long = 32-bit → managed int.
    /// </summary>
    [DllImport(Dll, EntryPoint = "OpenNetConnection", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = false)]
    internal static extern int OpenNetConnection(
        out IntPtr hComm,
        [MarshalAs(UnmanagedType.LPStr)] string strIp,
        ushort wPort,
        int timeoutMs);

    /// <summary>CFApi.h: int OpenHidConnection(int64_t* hComm, unsigned short index);</summary>
    [DllImport(Dll, EntryPoint = "OpenHidConnection", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int OpenHidConnection(out IntPtr hComm, ushort index);

    /// <summary>CFApi.h: int CloseDevice(int64_t hComm);</summary>
    [DllImport(Dll, EntryPoint = "CloseDevice", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int CloseDevice(IntPtr hComm);

    /// <summary>CFApi.h: int CFHid_GetUsbCount();</summary>
    [DllImport(Dll, EntryPoint = "CFHid_GetUsbCount", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int CFHid_GetUsbCount();

    /// <summary>
    /// CFApi.h: int CFHid_GetUsbInfo(unsigned short index, char* pucDeviceInfo);
    /// Caller must set <see cref="StringBuilder.Capacity"/> before invoke (Phase 3+).
    /// </summary>
    [DllImport(Dll, EntryPoint = "CFHid_GetUsbInfo", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = false)]
    internal static extern int CFHid_GetUsbInfo(ushort index, StringBuilder pucDeviceInfo);

    // ----------------- Device para / RF power -----------------

    /// <summary>CFApi.h: int GetDevicePara(int64_t hComm, DevicePara* devInfo);</summary>
    [DllImport(Dll, EntryPoint = "GetDevicePara", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int GetDevicePara(IntPtr hComm, out NativeDevicePara devInfo);

    /// <summary>CFApi.h: int SetRFPower(int64_t hComm, unsigned char power, unsigned char reserved);</summary>
    [DllImport(Dll, EntryPoint = "SetRFPower", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int SetRFPower(IntPtr hComm, byte power, byte reserved);

    /// <summary>CFApi.h: int GetRFPower(int64_t hComm, unsigned char* power, unsigned char* reserved);</summary>
    [DllImport(Dll, EntryPoint = "GetRFPower", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int GetRFPower(IntPtr hComm, out byte power, out byte reserved);

    // ----------------- Inventory -----------------

    /// <summary>CFApi.h: int InventoryContinue(int64_t hComm, unsigned char btInvCount, unsigned long dwInvParam);</summary>
    [DllImport(Dll, EntryPoint = "InventoryContinue", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int InventoryContinue(IntPtr hComm, byte btInvCount, uint dwInvParam);

    /// <summary>CFApi.h: int GetTagUii(int64_t hComm, TagInfo* tag_info, unsigned short timeout);</summary>
    [DllImport(Dll, EntryPoint = "GetTagUii", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int GetTagUii(IntPtr hComm, out NativeTagInfo tagInfo, ushort timeout);

    /// <summary>CFApi.h: int InventoryStop(int64_t hComm, unsigned short timeout);</summary>
    [DllImport(Dll, EntryPoint = "InventoryStop", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int InventoryStop(IntPtr hComm, ushort timeout);

    // ----------------- Select / Read / Write / Lock / Kill -----------------

    /// <summary>CFApi.h: int SetSelectMask(int64_t hComm, unsigned short maskPtr, unsigned char maskBits, unsigned char* mask);</summary>
    [DllImport(Dll, EntryPoint = "SetSelectMask", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int SetSelectMask(
        IntPtr hComm,
        ushort maskPtr,
        byte maskBits,
        [In] byte[] mask);

    /// <summary>
    /// CFApi.h: int WriteTag(int64_t hComm, unsigned char option, unsigned char* accPwd,
    /// unsigned char memBank, unsigned short wordPtr, unsigned char wordCount, unsigned char* writeData);
    /// </summary>
    [DllImport(Dll, EntryPoint = "WriteTag", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int WriteTag(
        IntPtr hComm,
        byte option,
        [In] byte[] accPwd,
        byte memBank,
        ushort wordPtr,
        byte wordCount,
        [In] byte[] writeData);

    /// <summary>CFApi.h: int GetTagResp(int64_t hComm, unsigned short cmd, TagResp* resp, unsigned short timeout);</summary>
    [DllImport(Dll, EntryPoint = "GetTagResp", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int GetTagResp(IntPtr hComm, ushort cmd, out NativeTagResp resp, ushort timeout);

    /// <summary>
    /// CFApi.h: int ReadTag(int64_t hComm, unsigned char option, unsigned char* accPwd,
    /// unsigned char memBank, unsigned short wordPtr, unsigned char wordCount);
    /// </summary>
    [DllImport(Dll, EntryPoint = "ReadTag", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int ReadTag(
        IntPtr hComm,
        byte option,
        [In] byte[] accPwd,
        byte memBank,
        ushort wordPtr,
        byte wordCount);

    /// <summary>
    /// CFApi.h: int GetReadTagResp(int64_t hComm, TagResp* resp, unsigned char* wordCount,
    /// unsigned char* readData, unsigned short timeout);
    /// Caller must pre-allocate <paramref name="readData"/> (Phase 3+).
    /// </summary>
    [DllImport(Dll, EntryPoint = "GetReadTagResp", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int GetReadTagResp(
        IntPtr hComm,
        out NativeTagResp resp,
        out byte wordCount,
        [Out] byte[] readData,
        ushort timeout);

    /// <summary>CFApi.h: int LockTag(int64_t hComm, unsigned char* accPwd, unsigned char erea, unsigned char action);</summary>
    [DllImport(Dll, EntryPoint = "LockTag", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int LockTag(
        IntPtr hComm,
        [In] byte[] accPwd,
        byte erea,
        byte action);

    /// <summary>CFApi.h: int KillTag(int64_t hComm, unsigned char* accPwd);</summary>
    [DllImport(Dll, EntryPoint = "KillTag", CallingConvention = CallingConvention.Winapi, ExactSpelling = true, SetLastError = false)]
    internal static extern int KillTag(IntPtr hComm, [In] byte[] accPwd);
}
