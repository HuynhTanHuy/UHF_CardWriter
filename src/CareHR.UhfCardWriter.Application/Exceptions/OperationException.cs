using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Exceptions;

/// <summary>
/// Thrown when an Application operation fails unexpectedly (device/port boundary mapped to Application).
/// </summary>
/// <remarks>Does not expose Sdk/Native exception types.</remarks>
public sealed class OperationException : Exception
{
    /// <summary>Initializes a new <see cref="OperationException"/>.</summary>
    public OperationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new <see cref="OperationException"/> with an error code.</summary>
    public OperationException(string message, DeviceErrorCode errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>Initializes a new <see cref="OperationException"/> with an inner exception.</summary>
    public OperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Gets the Application error code when known.</summary>
    public DeviceErrorCode ErrorCode { get; }
}
