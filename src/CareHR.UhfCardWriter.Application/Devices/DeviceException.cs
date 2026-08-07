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
    public DeviceException(string message)
        : base(message)
    {
    }

    public DeviceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
