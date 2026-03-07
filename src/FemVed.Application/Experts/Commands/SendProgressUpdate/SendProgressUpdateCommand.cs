using MediatR;

namespace FemVed.Application.Experts.Commands.SendProgressUpdate;

/// <summary>
/// Sends a progress comment from an expert or admin to a specific enrolled user.
/// Always dispatches an email to the enrolled user via SendGrid (<c>expert_progress_update</c> template).
/// </summary>
/// <param name="SenderUserId">The authenticated user's ID (expert user ID or admin user ID).</param>
/// <param name="AccessId">UUID of the UserProgramAccess record identifying the enrolled user.</param>
/// <param name="UpdateNote">The comment content to send.</param>
/// <param name="IsAdmin">True when the caller holds the Admin role (bypasses ownership check).</param>
public record SendProgressUpdateCommand(
    Guid SenderUserId,
    Guid AccessId,
    string UpdateNote,
    bool IsAdmin) : IRequest;
