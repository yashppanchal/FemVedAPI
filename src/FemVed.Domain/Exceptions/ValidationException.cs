namespace FemVed.Domain.Exceptions;

/// <summary>
/// Thrown when FluentValidation pipeline behaviour catches validation failures.
/// Maps to HTTP 400 Bad Request with a field-level error dictionary.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>Field-level validation errors keyed by property name.</summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>Initializes a new ValidationException with the supplied field errors.</summary>
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }
}
