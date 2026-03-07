using FemVed.Application.Experts.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Queries.GetEnrollmentComments;

/// <summary>
/// Handles <see cref="GetEnrollmentCommentsQuery"/>.
/// <list type="number">
///   <item>Verifies the access record exists.</item>
///   <item>For experts: verifies the caller owns the program (ExpertId matches).</item>
///   <item>Returns all ExpertProgressUpdate records for the access, oldest first.</item>
/// </list>
/// </summary>
public sealed class GetEnrollmentCommentsQueryHandler
    : IRequestHandler<GetEnrollmentCommentsQuery, List<EnrollmentCommentDto>>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ExpertProgressUpdate> _progressUpdates;
    private readonly ILogger<GetEnrollmentCommentsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetEnrollmentCommentsQueryHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<ExpertProgressUpdate> progressUpdates,
        ILogger<GetEnrollmentCommentsQueryHandler> logger)
    {
        _access          = access;
        _experts         = experts;
        _progressUpdates = progressUpdates;
        _logger          = logger;
    }

    /// <summary>Returns all comments for the enrollment, oldest first.</summary>
    /// <param name="request">The query payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when a non-admin caller does not own the program.</exception>
    public async Task<List<EnrollmentCommentDto>> Handle(
        GetEnrollmentCommentsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetEnrollmentComments: user {UserId} (isAdmin={IsAdmin}) fetching comments for access {AccessId}",
            request.RequestingUserId, request.IsAdmin, request.AccessId);

        var record = await _access.FirstOrDefaultAsync(a => a.Id == request.AccessId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        // ── Authorisation ─────────────────────────────────────────────────────
        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("You do not have an expert profile.");

            if (expert.Id != record.ExpertId)
                throw new ForbiddenException("You can only view comments for your own enrolled users.");
        }

        // ── Load comments ─────────────────────────────────────────────────────
        var comments = await _progressUpdates.GetAllAsync(
            p => p.AccessId == request.AccessId, cancellationToken);

        var result = comments
            .OrderBy(p => p.CreatedAt)
            .Select(p => new EnrollmentCommentDto(
                CommentId:  p.Id,
                AccessId:   p.AccessId,
                ExpertId:   p.ExpertId,
                UpdateNote: p.UpdateNote,
                CreatedAt:  p.CreatedAt))
            .ToList();

        _logger.LogInformation(
            "GetEnrollmentComments: returned {Count} comments for access {AccessId}",
            result.Count, request.AccessId);

        return result;
    }
}
