using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAllExperts;

/// <summary>Handles <see cref="GetAllExpertsQuery"/>. Returns all expert profiles with linked user email.</summary>
public sealed class GetAllExpertsQueryHandler : IRequestHandler<GetAllExpertsQuery, List<AdminExpertDto>>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<User> _users;
    private readonly ILogger<GetAllExpertsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllExpertsQueryHandler(
        IRepository<Expert> experts,
        IRepository<User> users,
        ILogger<GetAllExpertsQueryHandler> logger)
    {
        _experts = experts;
        _users   = users;
        _logger  = logger;
    }

    /// <summary>Returns all expert profiles ordered by creation date descending.</summary>
    public async Task<List<AdminExpertDto>> Handle(GetAllExpertsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllExperts: loading all experts");

        var experts = await _experts.GetAllAsync(cancellationToken: cancellationToken);

        var userIds = experts.Select(e => e.UserId).Distinct().ToHashSet();
        var users   = await _users.GetAllAsync(u => userIds.Contains(u.Id), cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var result = experts
            .OrderByDescending(e => e.CreatedAt)
            .Select(e =>
            {
                userMap.TryGetValue(e.UserId, out var user);
                return new AdminExpertDto(
                    ExpertId:        e.Id,
                    UserId:          e.UserId,
                    UserEmail:       user?.Email      ?? string.Empty,
                    DisplayName:     e.DisplayName,
                    Title:           e.Title,
                    LocationCountry: e.LocationCountry,
                    IsActive:        e.IsActive,
                    IsDeleted:       e.IsDeleted,
                    CreatedAt:       e.CreatedAt);
            })
            .ToList();

        _logger.LogInformation("GetAllExperts: returned {Count} experts", result.Count);
        return result;
    }
}
