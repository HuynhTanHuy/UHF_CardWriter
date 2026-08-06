namespace CareHR.UhfCardWriter.Application.Exceptions;

/// <summary>
/// Thrown when Application input fails business validation (before device/API calls).
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Initializes a new <see cref="ValidationException"/>.</summary>
    public ValidationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new <see cref="ValidationException"/> with an inner exception.</summary>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
