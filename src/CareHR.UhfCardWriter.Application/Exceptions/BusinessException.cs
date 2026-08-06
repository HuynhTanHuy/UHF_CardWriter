namespace CareHR.UhfCardWriter.Application.Exceptions;

/// <summary>
/// Thrown when a CareHR business rule is violated.
/// </summary>
public sealed class BusinessException : Exception
{
    /// <summary>Initializes a new <see cref="BusinessException"/>.</summary>
    public BusinessException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new <see cref="BusinessException"/> with an inner exception.</summary>
    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
