namespace FemVed.Domain.Exceptions;

/// <summary>Thrown when an authenticated user lacks permission for an action. Maps to HTTP 403 Forbidden.</summary>
public class ForbiddenException : Exception
{
    /// <summary>Initializes a new ForbiddenException with the default message.</summary>
    public ForbiddenException() : base("You do not have permission to perform this action.") { }

    /// <summary>Initializes a new ForbiddenException with the specified message.</summary>
    public ForbiddenException(string message) : base(message) { }
}
