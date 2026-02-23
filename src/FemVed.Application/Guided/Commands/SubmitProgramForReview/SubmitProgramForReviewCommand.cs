using MediatR;

namespace FemVed.Application.Guided.Commands.SubmitProgramForReview;

/// <summary>
/// Transitions a program from DRAFT to PENDING_REVIEW.
/// The expert submits their program for Admin review before it can be published.
/// </summary>
/// <param name="ProgramId">The program to submit.</param>
/// <param name="RequestingUserId">Authenticated user ID for ownership verification.</param>
/// <param name="IsAdmin">True when the caller is an Admin (bypasses ownership check).</param>
public record SubmitProgramForReviewCommand(Guid ProgramId, Guid RequestingUserId, bool IsAdmin) : IRequest;
