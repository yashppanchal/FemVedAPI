namespace FemVed.Domain.Exceptions;

/// <summary>Thrown when a request lacks valid authentication credentials. Maps to HTTP 401 Unauthorized.</summary>
public class UnauthorizedException : Exception
{
    /// <summary>Initializes a new UnauthorizedException with the default message.</summary>
    public UnauthorizedException() : base("Authentication is required.") { }

    /// <summary>Initializes a new UnauthorizedException with the specified message.</summary>
    public UnauthorizedException(string message) : base(message) { }
}
