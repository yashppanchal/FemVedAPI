using FemVed.Application.Experts.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Queries.GetAllExperts;

/// <summary>
/// Handles <see cref="GetAllExpertsQuery"/>.
/// Loads every expert and joins each one to its owning user account to surface the email.
/// </summary>
public sealed class GetAllExpertsQueryHandler : IRequestHandler<GetAllExpertsQuery, List<PublicExpertDto>>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<User> _users;
    private readonly ILogger<GetAllExpertsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetAllExpertsQueryHandler(
        IRepository<Expert> experts,
        IRepository<User> users,
        ILogger<GetAllExpertsQueryHandler> logger)
    {
        _experts = experts;
        _users   = users;
        _logger  = logger;
    }

    /// <summary>Returns every expert with their linked user email.</summary>
    public async Task<List<PublicExpertDto>> Handle(
        GetAllExpertsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllExperts: loading all expert profiles");

        var experts = await _experts.GetAllAsync(predicate: null, cancellationToken);
        if (experts.Count == 0)
        {
            _logger.LogInformation("GetAllExperts: no experts found");
            return new List<PublicExpertDto>();
        }

        var userIds = experts.Select(e => e.UserId).Distinct().ToHashSet();
        var users   = await _users.GetAllAsync(u => userIds.Contains(u.Id), cancellationToken);
        var emailByUserId = users.ToDictionary(u => u.Id, u => u.Email);

        var result = experts
            .OrderBy(e => e.DisplayName)
            .Select(e => new PublicExpertDto(
                UserEmail:           emailByUserId.TryGetValue(e.UserId, out var email) ? email : string.Empty,
                DisplayName:         e.DisplayName,
                Title:               e.Title,
                LocationCountry:     e.LocationCountry,
                IsActive:            e.IsActive,
                IsDeleted:           e.IsDeleted,
                CreatedAt:           e.CreatedAt,
                Bio:                 e.Bio,
                GridDescription:     e.GridDescription,
                DetailedDescription: e.DetailedDescription,
                ProfileImageUrl:     e.ProfileImageUrl,
                GridImageUrl:        e.GridImageUrl,
                Specialisations:     e.Specialisations,
                Credentials:         e.Credentials,
                YearsExperience:     e.YearsExperience))
            .ToList();

        _logger.LogInformation("GetAllExperts: returned {Count} expert(s)", result.Count);
        return result;
    }
}
