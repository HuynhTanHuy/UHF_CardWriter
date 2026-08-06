namespace CareHR.UhfCardWriter.Sdk.Driver;

/// <summary>
/// Owned managed buffer for native OUT <c>byte*</c> parameters (for example GetReadTagResp).
/// </summary>
/// <remarks>
/// Uses <see cref="byte"/>[] — CLR marshaller pins during the P/Invoke call.
/// No <c>AllocHGlobal</c> is required for current <c>UhfPrimeNative</c> signatures.
/// See docs/NativeBufferPolicy.md.
/// </remarks>
public sealed class NativeBuffer : IDisposable
{
    private byte[]? _buffer;
    private bool _disposed;

    /// <summary>Allocates a managed buffer of the specified size.</summary>
    /// <param name="size">Buffer length in bytes; must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not greater than zero.</exception>
    public NativeBuffer(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Buffer size must be > 0.");
        _buffer = new byte[size];
    }

    /// <summary>Gets the buffer length in bytes.</summary>
    /// <exception cref="ObjectDisposedException">The buffer has been disposed.</exception>
    public int Length => Buffer.Length;

    /// <summary>Gets the underlying managed buffer for a single P/Invoke call.</summary>
    /// <exception cref="ObjectDisposedException">The buffer has been disposed.</exception>
    public byte[] Buffer =>
        _disposed || _buffer is null
            ? throw new ObjectDisposedException(nameof(NativeBuffer))
            : _buffer;

    /// <summary>Copies the first <paramref name="count"/> bytes into a new array.</summary>
    /// <param name="count">Number of bytes to copy.</param>
    /// <returns>Independent copy owned by the caller.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is outside 0..Length.</exception>
    /// <exception cref="ObjectDisposedException">The buffer has been disposed.</exception>
    public byte[] ToArray(int count)
    {
        if (count < 0 || count > Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        var copy = new byte[count];
        System.Buffer.BlockCopy(Buffer, 0, copy, 0, count);
        return copy;
    }

    /// <summary>Releases the managed buffer reference.</summary>
    /// <remarks>Does not free unmanaged memory (none is allocated).</remarks>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _buffer = null;
    }
}
