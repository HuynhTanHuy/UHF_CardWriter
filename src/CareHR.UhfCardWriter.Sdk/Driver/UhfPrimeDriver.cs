using System.Text;
using CareHR.UhfCardWriter.Sdk.Native;

namespace CareHR.UhfCardWriter.Sdk.Driver;

/// <summary>
/// Low-level native driver for UHFPrimeReader.dll.
/// Owns the device handle, marshals buffers/structs, maps SDK status → <see cref="NativeResult"/>.
/// </summary>
/// <remarks>
/// <para>Not thread-safe. No inventory loops, retry, verify, logging, or business rules.</para>
/// <para>Only this type may call <c>UhfPrimeNative</c>. Handle and native structs are never exposed.</para>
/// <para>See docs/DriverContract.md and docs/ExceptionPolicy.md.</para>
/// </remarks>
public sealed class UhfPrimeDriver : IDisposable
{
    /// <summary>Required length of Gen2 access password buffer in bytes.</summary>
    public const int AccessPasswordLength = 4;

    /// <summary>Default capacity for USB info <see cref="StringBuilder"/>.</summary>
    public const int DefaultUsbInfoCapacity = 256;

    /// <summary>Default OUT buffer size for <see cref="GetReadTagResp"/>.</summary>
    public const int DefaultReadDataCapacity = 512;

    private IntPtr _handle = IntPtr.Zero;
    private bool _disposed;

    /// <summary>Gets a value indicating whether this instance currently owns an open reader handle.</summary>
    /// <remarks>The handle value itself is never exposed.</remarks>
    public bool IsOpen => _handle != IntPtr.Zero && !_disposed;

    // ----------------- Connection -----------------

    /// <summary>Opens a serial (COM) connection to the reader.</summary>
    /// <param name="comPort">COM port name (for example <c>COM3</c>).</param>
    /// <param name="baudRate">Baud rate; must be greater than zero.</param>
    /// <returns>SDK status as <see cref="NativeResult"/>. On success, this instance owns the handle.</returns>
    /// <exception cref="ArgumentException"><paramref name="comPort"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baudRate"/> is not greater than zero.</exception>
    /// <exception cref="NativeException">A handle is already open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Does not start inventory or configure RF.</remarks>
    public NativeResult OpenDevice(string comPort, int baudRate)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(comPort))
            throw new ArgumentException("COM port is required.", nameof(comPort));
        if (baudRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(baudRate));
        EnsureClosedForOpen();

        var status = UhfPrimeNative.OpenDevice(out var handle, comPort.Trim(), baudRate);
        if (status == NativeConstants.StatOk)
            _handle = handle;
        return NativeResult.FromStatus(status);
    }

    /// <summary>Opens a USB HID connection by device index.</summary>
    /// <param name="index">Zero-based USB device index from enumeration helpers.</param>
    /// <returns>SDK status as <see cref="NativeResult"/>. On success, this instance owns the handle.</returns>
    /// <exception cref="NativeException">A handle is already open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Use <see cref="GetHidUsbCount"/> / <see cref="GetHidUsbInfo"/> to discover indices. Does not require a prior open handle.</remarks>
    public NativeResult OpenHid(ushort index)
    {
        ThrowIfDisposed();
        EnsureClosedForOpen();

        var status = UhfPrimeNative.OpenHidConnection(out var handle, index);
        if (status == NativeConstants.StatOk)
            _handle = handle;
        return NativeResult.FromStatus(status);
    }

    /// <summary>Opens a TCP network connection to the reader.</summary>
    /// <param name="ip">Device IPv4/IPv6 address string.</param>
    /// <param name="port">TCP port; must be non-zero.</param>
    /// <param name="timeoutMs">Connect timeout in milliseconds; must be non-negative.</param>
    /// <returns>SDK status as <see cref="NativeResult"/>. On success, this instance owns the handle.</returns>
    /// <exception cref="ArgumentException"><paramref name="ip"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="port"/> is zero, or <paramref name="timeoutMs"/> is negative.</exception>
    /// <exception cref="NativeException">A handle is already open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    public NativeResult OpenNet(string ip, ushort port, int timeoutMs)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(ip))
            throw new ArgumentException("IP is required.", nameof(ip));
        if (port == 0)
            throw new ArgumentOutOfRangeException(nameof(port));
        if (timeoutMs < 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMs));
        EnsureClosedForOpen();

        var status = UhfPrimeNative.OpenNetConnection(out var handle, ip.Trim(), port, timeoutMs);
        if (status == NativeConstants.StatOk)
            _handle = handle;
        return NativeResult.FromStatus(status);
    }

    /// <summary>Closes the device and clears the owned handle.</summary>
    /// <returns>
    /// <see cref="NativeResult.Ok"/> if no handle was open; otherwise the SDK status from close.
    /// The owned handle is cleared after the call.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Prefer this over <see cref="Dispose"/> when the close status must be observed.</remarks>
    public NativeResult Close()
    {
        ThrowIfDisposed();
        if (_handle == IntPtr.Zero)
            return NativeResult.Ok();

        var status = UhfPrimeNative.CloseDevice(_handle);
        _handle = IntPtr.Zero;
        return NativeResult.FromStatus(status);
    }

    /// <summary>Returns the number of USB HID reader devices.</summary>
    /// <returns><see cref="NativeResult{T}"/> with count on success; does not require an open handle.</returns>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    public NativeResult<int> GetHidUsbCount()
    {
        ThrowIfDisposed();
        // CFHid_GetUsbCount returns count directly (non-negative) on success paths used by sample.
        var count = UhfPrimeNative.CFHid_GetUsbCount();
        if (count < 0)
            return NativeResult<int>.FromStatus(count);
        return NativeResult<int>.Ok(count);
    }

    /// <summary>Returns USB device info text for the given index.</summary>
    /// <param name="index">Zero-based USB device index.</param>
    /// <param name="capacity">Pre-allocated <see cref="StringBuilder"/> capacity; must be greater than zero.</param>
    /// <returns><see cref="NativeResult{T}"/> with info string on success; does not require an open handle.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not greater than zero.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    public NativeResult<string> GetHidUsbInfo(ushort index, int capacity = DefaultUsbInfoCapacity)
    {
        ThrowIfDisposed();
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        var sb = new StringBuilder(capacity);
        var status = UhfPrimeNative.CFHid_GetUsbInfo(index, sb);
        if (status != NativeConstants.StatOk)
            return NativeResult<string>.FromStatus(status);
        return NativeResult<string>.Ok(sb.ToString());
    }

    // ----------------- Inventory (single calls only) -----------------

    /// <summary>Issues a single <c>InventoryContinue</c> call.</summary>
    /// <param name="invCount">SDK inventory count parameter.</param>
    /// <param name="invParam">SDK inventory parameter.</param>
    /// <returns>SDK status as <see cref="NativeResult"/>.</returns>
    /// <exception cref="NativeException">The reader handle is not open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Does not loop. Caller polls <see cref="GetTagUii"/> and eventually <see cref="InventoryStop"/>.</remarks>
    public NativeResult InventoryContinue(byte invCount = 0, uint invParam = 0)
    {
        var handle = RequireHandle();
        return NativeResult.FromStatus(UhfPrimeNative.InventoryContinue(handle, invCount, invParam));
    }

    /// <summary>Issues a single <c>InventoryStop</c> call.</summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default 10000).</param>
    /// <returns>SDK status as <see cref="NativeResult"/>.</returns>
    /// <exception cref="NativeException">The reader handle is not open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    public NativeResult InventoryStop(ushort timeoutMs = 10000)
    {
        var handle = RequireHandle();
        return NativeResult.FromStatus(UhfPrimeNative.InventoryStop(handle, timeoutMs));
    }

    /// <summary>Polls one inventory tag identity and marshals it to a managed DTO.</summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns><see cref="NativeResult{T}"/> with <see cref="TagIdentityNative"/> on success.</returns>
    /// <exception cref="NativeException">Handle not open, or marshal mapping failed.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Does not run an inventory loop. Native <c>TagInfo</c> is not exposed.</remarks>
    public NativeResult<TagIdentityNative> GetTagUii(ushort timeoutMs)
    {
        var handle = RequireHandle();
        var status = UhfPrimeNative.GetTagUii(handle, out var native, timeoutMs);
        if (status != NativeConstants.StatOk)
            return NativeResult<TagIdentityNative>.FromStatus(status);

        try
        {
            return NativeResult<TagIdentityNative>.Ok(MapTagInfo(native));
        }
        catch (Exception ex)
        {
            throw new NativeException("Failed to marshal TagInfo to managed identity.", ex);
        }
    }

    // ----------------- Tag access -----------------

    /// <summary>Sets the Gen2 select mask.</summary>
    /// <param name="maskPtr">Mask bit pointer per SDK.</param>
    /// <param name="maskBits">Number of mask bits.</param>
    /// <param name="mask">Mask bytes; length must be at least ceil(maskBits/8).</param>
    /// <returns>SDK status as <see cref="NativeResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mask"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="mask"/> is shorter than required for <paramref name="maskBits"/>.</exception>
    /// <exception cref="NativeException">The reader handle is not open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    public NativeResult SetSelectMask(ushort maskPtr, byte maskBits, byte[] mask)
    {
        if (mask is null)
            throw new ArgumentNullException(nameof(mask));

        var requiredBytes = (maskBits + 7) / 8;
        if (mask.Length < requiredBytes)
            throw new ArgumentException($"Mask buffer length {mask.Length} < required {requiredBytes} for maskBits={maskBits}.", nameof(mask));

        var handle = RequireHandle();
        return NativeResult.FromStatus(UhfPrimeNative.SetSelectMask(handle, maskPtr, maskBits, mask));
    }

    /// <summary>Issues a single <c>WriteTag</c> command.</summary>
    /// <param name="option">SDK write option byte.</param>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="memBank">Gen2 bank: 0=Reserved, 1=EPC/UII, 2=TID, 3=User.</param>
    /// <param name="wordPtr">Starting word pointer.</param>
    /// <param name="writeData">Non-empty even-length payload (16-bit words).</param>
    /// <returns>SDK status as <see cref="NativeResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="accessPassword"/> or <paramref name="writeData"/> is null.</exception>
    /// <exception cref="ArgumentException">Password length invalid, or write data empty/odd length.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="memBank"/> &gt; 3, or word count exceeds 255.</exception>
    /// <exception cref="NativeException">The reader handle is not open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Does not call <see cref="GetTagResp"/> and does not verify written data.</remarks>
    public NativeResult WriteTag(
        byte option,
        byte[] accessPassword,
        byte memBank,
        ushort wordPtr,
        byte[] writeData)
    {
        ValidateAccessPassword(accessPassword);
        ValidateMemBank(memBank);
        if (writeData is null)
            throw new ArgumentNullException(nameof(writeData));
        if (writeData.Length == 0)
            throw new ArgumentException("Write data is empty.", nameof(writeData));
        if (writeData.Length % 2 != 0)
            throw new ArgumentException("Write data length must be an even number of bytes (16-bit words).", nameof(writeData));
        if (writeData.Length / 2 > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(writeData), "Word count exceeds byte range.");

        var wordCount = (byte)(writeData.Length / 2);
        var handle = RequireHandle();
        return NativeResult.FromStatus(
            UhfPrimeNative.WriteTag(
                handle,
                option,
                accessPassword,
                memBank,
                wordPtr,
                wordCount,
                writeData));
    }

    /// <summary>Polls an access-command response and marshals it to a managed DTO.</summary>
    /// <param name="cmd">ISO/access command id expected by the SDK (for example write/lock/kill).</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns><see cref="NativeResult{T}"/> with <see cref="TagResponseNative"/> on success.</returns>
    /// <exception cref="NativeException">Handle not open, or marshal mapping failed.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Native <c>TagResp</c> is not exposed.</remarks>
    public NativeResult<TagResponseNative> GetTagResp(ushort cmd, ushort timeoutMs)
    {
        var handle = RequireHandle();
        var status = UhfPrimeNative.GetTagResp(handle, cmd, out var native, timeoutMs);
        if (status != NativeConstants.StatOk)
            return NativeResult<TagResponseNative>.FromStatus(status);

        try
        {
            return NativeResult<TagResponseNative>.Ok(MapTagResp(native));
        }
        catch (Exception ex)
        {
            throw new NativeException("Failed to marshal TagResp to managed response.", ex);
        }
    }

    /// <summary>Issues a single <c>ReadTag</c> command.</summary>
    /// <param name="option">SDK read option byte.</param>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="memBank">Gen2 bank: 0=Reserved, 1=EPC/UII, 2=TID, 3=User.</param>
    /// <param name="wordPtr">Starting word pointer.</param>
    /// <param name="wordCount">Number of words to read; must be greater than zero.</param>
    /// <returns>SDK status as <see cref="NativeResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="accessPassword"/> is null.</exception>
    /// <exception cref="ArgumentException">Password length is not four.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="memBank"/> &gt; 3, or <paramref name="wordCount"/> is zero.</exception>
    /// <exception cref="NativeException">The reader handle is not open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Does not call <see cref="GetReadTagResp"/>.</remarks>
    public NativeResult ReadTag(
        byte option,
        byte[] accessPassword,
        byte memBank,
        ushort wordPtr,
        byte wordCount)
    {
        ValidateAccessPassword(accessPassword);
        ValidateMemBank(memBank);
        if (wordCount == 0)
            throw new ArgumentOutOfRangeException(nameof(wordCount));

        var handle = RequireHandle();
        return NativeResult.FromStatus(
            UhfPrimeNative.ReadTag(
                handle,
                option,
                accessPassword,
                memBank,
                wordPtr,
                wordCount));
    }

    /// <summary>Polls a read-tag response including payload bytes.</summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <param name="maxDataBytes">OUT buffer capacity; must be greater than zero.</param>
    /// <returns><see cref="NativeResult{T}"/> with <see cref="TagReadNative"/> on success.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDataBytes"/> is not greater than zero.</exception>
    /// <exception cref="NativeException">Handle not open, or marshal mapping failed.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Payload is copied; temporary <see cref="NativeBuffer"/> is disposed before return.</remarks>
    public NativeResult<TagReadNative> GetReadTagResp(ushort timeoutMs, int maxDataBytes = DefaultReadDataCapacity)
    {
        if (maxDataBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDataBytes));

        var handle = RequireHandle();
        using var buffer = new NativeBuffer(maxDataBytes);
        var status = UhfPrimeNative.GetReadTagResp(handle, out var native, out var wordCount, buffer.Buffer, timeoutMs);
        if (status != NativeConstants.StatOk)
            return NativeResult<TagReadNative>.FromStatus(status);

        try
        {
            var byteCount = Math.Min(buffer.Length, wordCount * 2);
            var data = buffer.ToArray(byteCount);
            var response = MapTagResp(native);
            return NativeResult<TagReadNative>.Ok(new TagReadNative(response, wordCount, data));
        }
        catch (Exception ex)
        {
            throw new NativeException("Failed to marshal GetReadTagResp payload.", ex);
        }
    }

    /// <summary>Issues a single <c>LockTag</c> command.</summary>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="area">Lock area per SDK.</param>
    /// <param name="action">Lock action per SDK.</param>
    /// <returns>SDK status as <see cref="NativeResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="accessPassword"/> is null.</exception>
    /// <exception cref="ArgumentException">Password length is not four.</exception>
    /// <exception cref="NativeException">The reader handle is not open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Does not call <see cref="GetTagResp"/>.</remarks>
    public NativeResult LockTag(byte[] accessPassword, byte area, byte action)
    {
        ValidateAccessPassword(accessPassword);
        var handle = RequireHandle();
        return NativeResult.FromStatus(UhfPrimeNative.LockTag(handle, accessPassword, area, action));
    }

    /// <summary>Issues a single <c>KillTag</c> command.</summary>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <returns>SDK status as <see cref="NativeResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="accessPassword"/> is null.</exception>
    /// <exception cref="ArgumentException">Password length is not four.</exception>
    /// <exception cref="NativeException">The reader handle is not open.</exception>
    /// <exception cref="ObjectDisposedException">The driver has been disposed.</exception>
    /// <remarks>Does not call <see cref="GetTagResp"/>.</remarks>
    public NativeResult KillTag(byte[] accessPassword)
    {
        ValidateAccessPassword(accessPassword);
        var handle = RequireHandle();
        return NativeResult.FromStatus(UhfPrimeNative.KillTag(handle, accessPassword));
    }

    // ----------------- Dispose -----------------

    /// <summary>Releases the owned reader handle (best-effort close).</summary>
    /// <remarks>Does not throw. Prefer <see cref="Close"/> when close status must be observed.</remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_handle != IntPtr.Zero)
        {
            try
            {
                UhfPrimeNative.CloseDevice(_handle);
            }
            catch
            {
                // Best-effort close on dispose; do not throw from Dispose.
            }

            _handle = IntPtr.Zero;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // ----------------- Mapping / guards -----------------

    private IntPtr RequireHandle()
    {
        ThrowIfDisposed();
        if (_handle == IntPtr.Zero)
            throw new NativeException("Reader handle is not open.");
        return _handle;
    }

    private void EnsureClosedForOpen()
    {
        if (_handle != IntPtr.Zero)
            throw new NativeException("Reader handle is already open. Close before opening again.");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UhfPrimeDriver));
    }

    private static void ValidateAccessPassword(byte[] accessPassword)
    {
        if (accessPassword is null)
            throw new ArgumentNullException(nameof(accessPassword));
        if (accessPassword.Length != AccessPasswordLength)
            throw new ArgumentException($"Access password must be exactly {AccessPasswordLength} bytes.", nameof(accessPassword));
    }

    private static void ValidateMemBank(byte memBank)
    {
        if (memBank > (byte)NativeMemBank.User)
            throw new ArgumentOutOfRangeException(nameof(memBank), "memBank must be 0..3 (Reserved/EPC/TID/User).");
    }

    private static TagIdentityNative MapTagInfo(NativeTagInfo native)
    {
        var len = native.CodeLength;
        var epc = CopyBytes(native.Code, len);
        var crc = CopyBytes(native.Crc, 2);
        var pc = CopyBytes(native.Pc, 2);
        return new TagIdentityNative(native.NO, native.Rssi, native.Antenna, native.Channel, crc, pc, epc);
    }

    private static TagResponseNative MapTagResp(NativeTagResp native)
    {
        var len = native.CodeLength;
        var code = CopyBytes(native.Code, len);
        var crc = CopyBytes(native.Crc, 2);
        var pc = CopyBytes(native.Pc, 2);
        return new TagResponseNative(native.TagStatus, native.Antenna, crc, pc, code);
    }

    private static byte[] CopyBytes(byte[]? source, int length)
    {
        if (source is null || length <= 0)
            return Array.Empty<byte>();
        if (length > source.Length)
            length = source.Length;
        var copy = new byte[length];
        Buffer.BlockCopy(source, 0, copy, 0, length);
        return copy;
    }
}
