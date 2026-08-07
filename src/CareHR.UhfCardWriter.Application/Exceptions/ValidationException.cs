namespace CareHR.UhfCardWriter.Application.Exceptions;

/// <summary>
/// Thrown when Application input fails business validation (before device/API calls).
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
