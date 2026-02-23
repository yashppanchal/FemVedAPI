using MediatR;

namespace FemVed.Application.Users.Commands.RequestGdprDeletion;

/// <summary>
/// Submits a GDPR right-to-erasure request for the authenticated user.
/// Intended for users in GB/EU. Stored and processed manually by an Admin.
/// Duplicate requests (while one is still Pending) are silently ignored.
/// </summary>
/// <param name="UserId">The authenticated user's ID (injected from JWT by the controller).</param>
public record RequestGdprDeletionCommand(Guid UserId) : IRequest;
