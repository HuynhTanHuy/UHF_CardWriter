using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Exceptions;

/// <summary>
/// Thrown when an Application operation fails unexpectedly (device/port boundary mapped to Application).
/// </summary>
/// <remarks>Does not expose Sdk/Native exception types.</remarks>
public sealed class OperationException : Exception
{
    public OperationException(string message)
        : base(message)
    {
    }

    public OperationException(string message, DeviceErrorCode errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public OperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DeviceErrorCode ErrorCode { get; }
}
