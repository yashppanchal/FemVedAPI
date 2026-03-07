using FemVed.Application.Experts.DTOs;
using MediatR;

namespace FemVed.Application.Experts.Queries.GetEnrollmentComments;

/// <summary>
/// Returns all comments (progress updates) sent for a specific enrollment.
/// Callers must be the expert who owns the program, or an admin.
/// </summary>
/// <param name="AccessId">UUID of the UserProgramAccess record whose comments to retrieve.</param>
/// <param name="RequestingUserId">Authenticated user's ID.</param>
/// <param name="IsAdmin">True when the caller holds the Admin role.</param>
public record GetEnrollmentCommentsQuery(
    Guid AccessId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest<List<EnrollmentCommentDto>>;
