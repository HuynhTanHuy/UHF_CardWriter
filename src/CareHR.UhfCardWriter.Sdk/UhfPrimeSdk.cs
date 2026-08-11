using CareHR.UhfCardWriter.Sdk.Driver;
using CareHR.UhfCardWriter.Sdk.Models;
using CareHR.UhfCardWriter.Sdk.Native;

namespace CareHR.UhfCardWriter.Sdk;

/// <summary>
/// Default UHF SDK Wrapper implementation. Owns one <see cref="UhfPrimeDriver"/> instance.
/// </summary>
/// <remarks>
/// <para>Public surface uses <see cref="SdkResult"/> and managed models only.</para>
/// <para>Not thread-safe. No retry, logging, verify, or inventory polling loops.</para>
/// <para>See docs/SDKWrapperContract.md.</para>
/// </remarks>
public sealed class UhfPrimeSdk : IUhfSdk, IUhfConnection, IUhfInventory, IUhfWriter, IUhfReader, IUhfTagControl
{
    private readonly UhfPrimeDriver _driver;
    private readonly bool _ownsDriver;
    private bool _disposed;

    /// <summary>Creates a new SDK instance that owns its Driver.</summary>
    public UhfPrimeSdk()
        : this(new UhfPrimeDriver(), ownsDriver: true)
    {
    }

    /// <summary>
    /// Creates a new SDK instance around an existing Driver (same-assembly / test hosts).
    /// </summary>
    /// <param name="driver">Driver instance.</param>
    /// <param name="ownsDriver">When true, <see cref="Dispose"/> disposes the Driver.</param>
    /// <exception cref="ArgumentNullException"><paramref name="driver"/> is null.</exception>
    /// <remarks>Public consumers should use the parameterless constructor and <see cref="IUhfSdk"/> only.</remarks>
    internal UhfPrimeSdk(UhfPrimeDriver driver, bool ownsDriver = false)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _ownsDriver = ownsDriver;
    }

    /// <inheritdoc />
    public IUhfConnection Connection => this;

    /// <inheritdoc />
    public IUhfInventory Inventory => this;

    /// <inheritdoc />
    public IUhfWriter Writer => this;

    /// <inheritdoc />
    public IUhfReader Reader => this;

    /// <inheritdoc />
    public IUhfTagControl TagControl => this;

    // ----------------- IUhfConnection -----------------

    /// <inheritdoc />
    public bool IsOpen
    {
        get
        {
            ThrowIfDisposed();
            return _driver.IsOpen;
        }
    }

    /// <inheritdoc />
    public SdkResult OpenSerial(string comPort, int baudRate)
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.OpenDevice(comPort, baudRate));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult OpenHid(ushort index)
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.OpenHid(index));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult OpenNet(string ip, ushort port, int timeoutMs)
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.OpenNet(ip, port, timeoutMs));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult Close()
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.Close());
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult<int> GetUsbDeviceCount()
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.GetHidUsbCount());
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult<string> GetUsbDeviceInfo(ushort index, int capacity = SdkConstants.DefaultUsbInfoCapacity)
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.GetHidUsbInfo(index, capacity));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult<DeviceParameters> GetDevicePara()
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.GetDevicePara());
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult SetDevicePara(DeviceParameters para)
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.SetDevicePara(para));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    // ----------------- IUhfInventory -----------------

    /// <inheritdoc />
    public SdkResult Start(byte invCount = 0, uint invParam = 0)
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.InventoryContinue(invCount, invParam));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult Stop(ushort timeoutMs = SdkConstants.DefaultInventoryStopTimeoutMs)
    {
        ThrowIfDisposed();
        try
        {
            WriteDiag(
                $"[Write] InventoryStopStart Timeout={timeoutMs} ThreadId={Environment.CurrentManagedThreadId}");
            var stop = _driver.InventoryStop(timeoutMs);
            WriteDiag(
                $"[Write] InventoryStopResult Status=0x{stop.StatusCode:X8} Success={stop.Success} Message={stop.Message}");
            return Map(stop);
        }
        catch (NativeException ex)
        {
            WriteDiag($"[Write] InventoryStopResult EXCEPTION {ex.Message}");
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult<TagIdentity> GetCurrentTag(ushort timeoutMs)
    {
        ThrowIfDisposed();
        try
        {
            var result = _driver.GetTagUii(timeoutMs);
            if (!result.Success || result.Value is null)
                return Fail<TagIdentity>(result.StatusCode, result.Message);

            return Ok(result.StatusCode, result.Message, ToTagIdentity(result.Value));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    // ----------------- IUhfWriter -----------------

    /// <inheritdoc />
    public SdkResult<TagAccessResponse> Write(
        byte option,
        byte[] accessPassword,
        MemBank memBank,
        ushort wordPtr,
        byte[] writeData,
        ushort responseTimeoutMs = SdkConstants.DefaultWriteResponseTimeoutMs)
    {
        ThrowIfDisposed();
        try
        {
            var dataLen = writeData is null ? 0 : writeData.Length;
            var wordCount = dataLen / 2;
            var pwdLen = accessPassword is null ? 0 : accessPassword.Length;
            var dataHex = writeData is null || writeData.Length == 0
                ? "(empty)"
                : Convert.ToHexString(writeData);
            var threadId = Environment.CurrentManagedThreadId;
            var sleep20 = IsSleep20DiagEnabled();
            var tag = sleep20 ? "[TEST-SLEEP20]" : "[Write]";

            WriteDiag(
                $"{tag} WriteTagStart Option={option} Bank={memBank} WordPtr={wordPtr} " +
                $"DataLength={dataLen} WordCount={wordCount} DataHex={dataHex} " +
                $"PasswordPresent={(pwdLen > 0)} PasswordLength={pwdLen} Timeout={responseTimeoutMs} " +
                $"ThreadId={threadId} DiagMode={(sleep20 ? "Sleep20" : "Baseline")}");

            var write = _driver.WriteTag(option, accessPassword!, (byte)memBank, wordPtr, writeData!);
            WriteDiag(
                $"{tag} WriteTagResult Status=0x{write.StatusCode:X8} Success={write.Success} Message={write.Message}");
            if (!write.Success)
            {
                WriteDiag($"{tag} FinalWriteResult=FAIL at WriteTag Status=0x{write.StatusCode:X8}");
                if (sleep20)
                    WriteDiag($"{tag} Final=FAIL");
                return Fail<TagAccessResponse>(write.StatusCode, write.Message);
            }

            // Controlled experiment R8 only: CAREHR_UHF_WRITE_TEST=Sleep20 inserts vendor Sleep(20).
            // No polling, no other algorithm changes. Default/baseline path stays unchanged.
            if (sleep20)
            {
                WriteDiag($"{tag} SleepMs=20");
                Thread.Sleep(20);
            }

            var respSw = System.Diagnostics.Stopwatch.StartNew();
            WriteDiag(
                $"{tag} GetTagRespStart Command=0x{NativeConstants.IsoWriteTag:X4} Timeout={responseTimeoutMs} " +
                $"ThreadId={threadId}");
            var resp = _driver.GetTagResp(NativeConstants.IsoWriteTag, responseTimeoutMs);
            respSw.Stop();
            WriteDiag(
                $"{tag} GetTagRespResult Status=0x{resp.StatusCode:X8} Success={resp.Success} Message={resp.Message}");
            WriteDiag($"{tag} GetTagRespElapsedMs={respSw.ElapsedMilliseconds}");
            if (!resp.Success || resp.Value is null)
            {
                WriteDiag($"{tag} FinalWriteResult=FAIL at GetTagResp Status=0x{resp.StatusCode:X8}");
                if (sleep20)
                    WriteDiag($"{tag} Final=FAIL");
                return Fail<TagAccessResponse>(resp.StatusCode, resp.Message);
            }

            WriteDiag($"{tag} FinalWriteResult=OK Status=0x{resp.StatusCode:X8}");
            if (sleep20)
                WriteDiag($"{tag} Final=PASS");
            return Ok(resp.StatusCode, resp.Message, ToTagAccessResponse(resp.Value));
        }
        catch (NativeException ex)
        {
            var tag = IsSleep20DiagEnabled() ? "[TEST-SLEEP20]" : "[Write]";
            WriteDiag($"{tag} FinalWriteResult=EXCEPTION {ex.Message}");
            if (IsSleep20DiagEnabled())
                WriteDiag($"{tag} Final=FAIL");
            throw new SdkException(ex.Message, ex);
        }
    }

    // ----------------- IUhfReader -----------------

    /// <inheritdoc />
    public SdkResult<TagReadData> Read(
        byte option,
        byte[] accessPassword,
        MemBank memBank,
        ushort wordPtr,
        byte wordCount,
        ushort responseTimeoutMs = SdkConstants.DefaultReadResponseTimeoutMs)
    {
        ThrowIfDisposed();
        try
        {
            var pwdLen = accessPassword is null ? 0 : accessPassword.Length;
            WriteDiag(
                $"[Verify] ReadTagStart Option={option} Bank={memBank} WordPtr={wordPtr} " +
                $"WordCount={wordCount} PasswordPresent={(pwdLen > 0)} PasswordLength={pwdLen} Timeout={responseTimeoutMs}");

            var read = _driver.ReadTag(option, accessPassword!, (byte)memBank, wordPtr, wordCount);
            WriteDiag(
                $"[Verify] ReadTagResult Status=0x{read.StatusCode:X8} Success={read.Success} Message={read.Message}");
            WriteDiag(
                $"[VerifyTest] ReadTagResult Status=0x{read.StatusCode:X8} Success={read.Success} Message={read.Message}");
            if (!read.Success)
            {
                WriteDiag($"[Verify] FailAt=ReadTag Status=0x{read.StatusCode:X8}");
                WriteDiag($"[VerifyTest] FailAt=ReadTag Status=0x{read.StatusCode:X8}");
                return Fail<TagReadData>(read.StatusCode, read.Message);
            }

            var maxBytes = Math.Max(wordCount * 2, 2);
            // IsoReadTag=0x0003 is the Gen2 read command id (GetReadTagResp is the dedicated poll API).
            WriteDiag(
                $"[Verify] GetReadTagRespStart Command=0x{NativeConstants.IsoReadTag:X4} " +
                $"Timeout={responseTimeoutMs} MaxBytes={maxBytes}");
            var resp = _driver.GetReadTagResp(responseTimeoutMs, maxBytes);
            WriteDiag(
                $"[Verify] GetReadTagRespResult Status=0x{resp.StatusCode:X8} Success={resp.Success} Message={resp.Message}");
            WriteDiag(
                $"[VerifyTest] GetReadTagRespResult Status=0x{resp.StatusCode:X8} Success={resp.Success} Message={resp.Message}");
            if (!resp.Success || resp.Value is null)
            {
                WriteDiag($"[Verify] FailAt=GetReadTagResp Status=0x{resp.StatusCode:X8}");
                WriteDiag($"[VerifyTest] FailAt=GetReadTagResp Status=0x{resp.StatusCode:X8}");
                return Fail<TagReadData>(resp.StatusCode, resp.Message);
            }

            var dataLen = resp.Value.Data?.Length ?? 0;
            var dataHex = dataLen == 0 || resp.Value.Data is null
                ? "(empty)"
                : Convert.ToHexString(resp.Value.Data);
            WriteDiag($"[Verify] ReadDataLength={dataLen} ReadDataHex={dataHex}");
            WriteDiag($"[VerifyTest] ActualEpcHex={dataHex}");
            WriteDiag("[Verify] FailAt=NONE ReadPath=OK");
            return Ok(resp.StatusCode, resp.Message, ToTagReadData(resp.Value));
        }
        catch (NativeException ex)
        {
            WriteDiag($"[Verify] FailAt=EXCEPTION {ex.Message}");
            WriteDiag($"[VerifyTest] FailAt=EXCEPTION {ex.Message}");
            throw new SdkException(ex.Message, ex);
        }
    }

    // ----------------- IUhfTagControl -----------------

    /// <inheritdoc />
    public SdkResult Select(ushort maskPtr, byte maskBits, byte[] mask)
    {
        ThrowIfDisposed();
        try
        {
            var maskEpc = mask is null || mask.Length == 0 ? "UNKNOWN" : Convert.ToHexString(mask);
            WriteDiag(
                $"[Write] SelectStart MaskEpc={maskEpc} MaskLength={(mask?.Length ?? 0)} " +
                $"MaskPtr={maskPtr} MaskBits={maskBits} ThreadId={Environment.CurrentManagedThreadId}");
            var result = _driver.SetSelectMask(maskPtr, maskBits, mask!);
            WriteDiag(
                $"[Write] SelectResult Status=0x{result.StatusCode:X8} Success={result.Success} Message={result.Message}");
            return Map(result);
        }
        catch (NativeException ex)
        {
            WriteDiag($"[Write] SelectResult EXCEPTION {ex.Message}");
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult Lock(byte[] accessPassword, byte area, byte action)
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.LockTag(accessPassword, area, action));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public SdkResult Kill(byte[] accessPassword)
    {
        ThrowIfDisposed();
        try
        {
            return Map(_driver.KillTag(accessPassword));
        }
        catch (NativeException ex)
        {
            throw new SdkException(ex.Message, ex);
        }
    }

    // ----------------- Dispose -----------------

    /// <summary>Disposes the owned Driver when applicable.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsDriver)
            _driver.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // ----------------- Mapping -----------------

    /// <summary>
    /// Controlled R8 experiment only. Set env <c>CAREHR_UHF_WRITE_TEST=Sleep20</c> to insert
    /// <see cref="Thread.Sleep(int)"/> 20ms between WriteTag and GetTagResp. Default = baseline (no sleep).
    /// </summary>
    internal static bool IsSleep20DiagEnabled()
    {
        var mode = Environment.GetEnvironmentVariable("CAREHR_UHF_WRITE_TEST");
        return string.Equals(mode, "Sleep20", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Temporary Write-path diagnostic (Select / WriteTag / GetTagResp). Does not change algorithm
    /// unless <see cref="IsSleep20DiagEnabled"/> is set for the R8 controlled test.
    /// Writes to %LocalAppData%\CareHR\UhfCardWriter\logs\write-diag.log — never logs password bytes.
    /// </summary>
    private static void WriteDiag(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CareHR",
                "UhfCardWriter",
                "logs");
            Directory.CreateDirectory(dir);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{message}";
            File.AppendAllText(Path.Combine(dir, "write-diag.log"), line + Environment.NewLine);
            System.Diagnostics.Trace.WriteLine(line);
        }
        catch
        {
            // Diagnostic only — never fail Write because of logging.
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UhfPrimeSdk));
    }

    private static SdkResult Map(NativeResult result) =>
        new(result.StatusCode, result.Success, result.Message);

    private static SdkResult<T> Map<T>(NativeResult<T> result) =>
        new(result.StatusCode, result.Success, result.Message, result.Value);

    private static SdkResult<T> Ok<T>(int statusCode, string message, T value) =>
        new(statusCode, true, message, value);

    private static SdkResult<T> Fail<T>(int statusCode, string message) =>
        new(statusCode, false, message, default);

    private static TagIdentity ToTagIdentity(TagIdentityNative native) =>
        new(
            native.NO,
            native.RssiTenthsDbm,
            native.Antenna,
            native.Channel,
            Copy(native.Crc),
            Copy(native.Pc),
            Copy(native.Epc));

    private static TagAccessResponse ToTagAccessResponse(TagResponseNative native) =>
        new(
            native.TagStatus,
            native.Antenna,
            Copy(native.Crc),
            Copy(native.Pc),
            Copy(native.Code));

    private static TagReadData ToTagReadData(TagReadNative native) =>
        new(
            ToTagAccessResponse(native.Response),
            native.WordCount,
            Copy(native.Data));

    private static byte[] Copy(byte[]? source)
    {
        if (source is null || source.Length == 0)
            return Array.Empty<byte>();
        var copy = new byte[source.Length];
        Buffer.BlockCopy(source, 0, copy, 0, source.Length);
        return copy;
    }
}
