namespace FemVed.Domain.Exceptions;

/// <summary>Base exception for domain rule violations. Maps to HTTP 422 Unprocessable Entity.</summary>
public class DomainException : Exception
{
    /// <summary>Initializes a new instance of <see cref="DomainException"/> with the specified message.</summary>
    public DomainException(string message) : base(message) { }

    /// <summary>Initializes a new instance of <see cref="DomainException"/> with the specified message and inner exception.</summary>
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
