namespace CareHR.UhfCardWriter.Application.Exceptions;

/// <summary>
/// Thrown when a CareHR business rule is violated.
/// </summary>
public sealed class BusinessException : Exception
{
    public BusinessException(string message)
        : base(message)
    {
    }

    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
