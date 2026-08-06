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
            return Map(_driver.InventoryStop(timeoutMs));
        }
        catch (NativeException ex)
        {
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
            var write = _driver.WriteTag(option, accessPassword, (byte)memBank, wordPtr, writeData);
            if (!write.Success)
                return Fail<TagAccessResponse>(write.StatusCode, write.Message);

            var resp = _driver.GetTagResp(NativeConstants.IsoWriteTag, responseTimeoutMs);
            if (!resp.Success || resp.Value is null)
                return Fail<TagAccessResponse>(resp.StatusCode, resp.Message);

            return Ok(resp.StatusCode, resp.Message, ToTagAccessResponse(resp.Value));
        }
        catch (NativeException ex)
        {
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
            var read = _driver.ReadTag(option, accessPassword, (byte)memBank, wordPtr, wordCount);
            if (!read.Success)
                return Fail<TagReadData>(read.StatusCode, read.Message);

            var maxBytes = Math.Max(wordCount * 2, 2);
            var resp = _driver.GetReadTagResp(responseTimeoutMs, maxBytes);
            if (!resp.Success || resp.Value is null)
                return Fail<TagReadData>(resp.StatusCode, resp.Message);

            return Ok(resp.StatusCode, resp.Message, ToTagReadData(resp.Value));
        }
        catch (NativeException ex)
        {
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
            return Map(_driver.SetSelectMask(maskPtr, maskBits, mask));
        }
        catch (NativeException ex)
        {
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
