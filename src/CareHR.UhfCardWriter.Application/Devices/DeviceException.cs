namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Thrown for device/session interop failures (not vendor status codes).
/// </summary>
/// <remarks>
/// Vendor status codes are returned via <see cref="DeviceResult"/> / <see cref="DeviceResult{T}"/>.
/// Infrastructure maps SDK <c>SdkException</c> to this type.
/// </remarks>
public sealed class DeviceException : Exception
{
    /// <summary>Initializes a new <see cref="DeviceException"/>.</summary>
    /// <param name="message">Error message.</param>
    public DeviceException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new <see cref="DeviceException"/> with an inner exception.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public DeviceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
