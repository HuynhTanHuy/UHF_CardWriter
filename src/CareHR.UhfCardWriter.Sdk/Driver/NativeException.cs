namespace CareHR.UhfCardWriter.Sdk.Driver;

/// <summary>
/// Thrown for interop / resource failures only (not SDK status codes).
/// </summary>
/// <remarks>
/// SDK status codes are returned via <see cref="NativeResult"/> / <see cref="NativeResult{T}"/>.
/// Typical causes: invalid handle, already open, marshal mapping failure.
/// See docs/ExceptionPolicy.md.
/// </remarks>
public sealed class NativeException : Exception
{
    /// <summary>Initializes a new <see cref="NativeException"/>.</summary>
    /// <param name="message">Error message.</param>
    public NativeException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new <see cref="NativeException"/> with an inner exception.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public NativeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
