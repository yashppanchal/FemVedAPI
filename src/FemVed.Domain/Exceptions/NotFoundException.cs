namespace FemVed.Domain.Exceptions;

/// <summary>Thrown when a requested resource does not exist. Maps to HTTP 404 Not Found.</summary>
public class NotFoundException : Exception
{
    /// <summary>Initializes a new NotFoundException for a named resource.</summary>
    /// <param name="resourceName">Human-readable name of the resource type (e.g. "Program").</param>
    /// <param name="id">Identifier that was searched for.</param>
    public NotFoundException(string resourceName, object id)
        : base($"{resourceName} with id '{id}' was not found.") { }

    /// <summary>Initializes a new NotFoundException with a custom message.</summary>
    public NotFoundException(string message) : base(message) { }
}
