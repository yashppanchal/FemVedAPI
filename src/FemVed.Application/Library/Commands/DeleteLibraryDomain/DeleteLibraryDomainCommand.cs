using MediatR;

namespace FemVed.Application.Library.Commands.DeleteLibraryDomain;

/// <summary>
/// Soft-deactivates a library domain (sets IsActive = false).
/// AdminOnly operation.
/// </summary>
/// <param name="DomainId">The domain to deactivate.</param>
public record DeleteLibraryDomainCommand(Guid DomainId) : IRequest;
