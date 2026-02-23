using MediatR;

namespace FemVed.Application.Experts.Commands.SendProgressUpdate;

/// <summary>
/// Sends a progress update note from an expert to a specific enrolled user.
/// Optionally also sends the note as an email via SendGrid (<c>expert_progress_update</c> template).
/// </summary>
/// <param name="ExpertId">The authenticated expert's ID (injected from JWT by the controller).</param>
/// <param name="AccessId">UUID of the UserProgramAccess record identifying the enrolled user.</param>
/// <param name="UpdateNote">The progress note content to send.</param>
/// <param name="SendEmail">When true, also dispatches an email to the enrolled user via SendGrid.</param>
public record SendProgressUpdateCommand(
    Guid ExpertId,
    Guid AccessId,
    string UpdateNote,
    bool SendEmail) : IRequest;
