namespace CareHR.UhfCardWriter.Sdk.Models;

/// <summary>
/// Thrown for SDK Wrapper resource / interop failures (not vendor status codes).
/// </summary>
/// <remarks>
/// Vendor status codes are returned via <see cref="SdkResult"/> / <see cref="SdkResult{T}"/>.
/// Typical causes: invalid session state, marshal failure from Driver.
/// </remarks>
public sealed class SdkException : Exception
{
    /// <summary>Initializes a new <see cref="SdkException"/>.</summary>
    /// <param name="message">Error message.</param>
    public SdkException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new <see cref="SdkException"/> with an inner exception.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public SdkException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
