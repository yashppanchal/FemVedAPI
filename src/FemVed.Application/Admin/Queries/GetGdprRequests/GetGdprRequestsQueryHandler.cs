using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetGdprRequests;

/// <summary>Handles <see cref="GetGdprRequestsQuery"/>.</summary>
public sealed class GetGdprRequestsQueryHandler : IRequestHandler<GetGdprRequestsQuery, List<GdprRequestDto>>
{
    private readonly IRepository<GdprDeletionRequest> _requests;
    private readonly IRepository<User> _users;
    private readonly ILogger<GetGdprRequestsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetGdprRequestsQueryHandler(
        IRepository<GdprDeletionRequest> requests,
        IRepository<User> users,
        ILogger<GetGdprRequestsQueryHandler> logger)
    {
        _requests = requests;
        _users    = users;
        _logger   = logger;
    }

    /// <summary>Returns GDPR requests filtered by status, ordered by submission date descending.</summary>
    public async Task<List<GdprRequestDto>> Handle(GetGdprRequestsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetGdprRequests: loading requests with status filter {Status}", request.Status);

        var all = request.Status.HasValue
            ? await _requests.GetAllAsync(r => r.Status == request.Status.Value, cancellationToken)
            : await _requests.GetAllAsync(cancellationToken: cancellationToken);

        var userIds = all.Select(r => r.UserId).Distinct().ToHashSet();
        var users   = await _users.GetAllAsync(u => userIds.Contains(u.Id), cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var result = all
            .OrderByDescending(r => r.RequestedAt)
            .Select(r =>
            {
                userMap.TryGetValue(r.UserId, out var user);
                return new GdprRequestDto(
                    RequestId:         r.Id,
                    UserId:            r.UserId,
                    UserEmail:         user?.Email     ?? string.Empty,
                    UserFirstName:     user?.FirstName ?? string.Empty,
                    UserLastName:      user?.LastName  ?? string.Empty,
                    Status:            r.Status.ToString(),
                    RequestedAt:       r.RequestedAt,
                    CompletedAt:       r.CompletedAt,
                    RejectionReason:   r.RejectionReason,
                    ProcessedByUserId: r.ProcessedBy);
            })
            .ToList();

        _logger.LogInformation("GetGdprRequests: returned {Count} requests", result.Count);
        return result;
    }
}
