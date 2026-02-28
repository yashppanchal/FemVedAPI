using FemVed.Application.Experts.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Queries.GetMyExpertProfile;

/// <summary>
/// Handles <see cref="GetMyExpertProfileQuery"/>.
/// Locates the expert record via the user's UserId FK and maps it to <see cref="ExpertProfileDto"/>.
/// </summary>
public sealed class GetMyExpertProfileQueryHandler : IRequestHandler<GetMyExpertProfileQuery, ExpertProfileDto>
{
    private readonly IRepository<Expert> _experts;
    private readonly ILogger<GetMyExpertProfileQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetMyExpertProfileQueryHandler(
        IRepository<Expert> experts,
        ILogger<GetMyExpertProfileQueryHandler> logger)
    {
        _experts = experts;
        _logger  = logger;
    }

    /// <summary>Fetches and returns the expert profile for the authenticated user.</summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The expert's profile DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when no expert profile exists for the user.</exception>
    public async Task<ExpertProfileDto> Handle(GetMyExpertProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyExpertProfile: loading expert profile for user {UserId}", request.UserId);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.UserId == request.UserId && !e.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Expert profile", request.UserId);

        _logger.LogInformation("GetMyExpertProfile: loaded expert {ExpertId}", expert.Id);

        return new ExpertProfileDto(
            ExpertId:           expert.Id,
            UserId:             expert.UserId,
            DisplayName:        expert.DisplayName,
            Title:              expert.Title,
            Bio:                expert.Bio,
            GridDescription:    expert.GridDescription,
            DetailedDescription:expert.DetailedDescription,
            ProfileImageUrl:    expert.ProfileImageUrl,
            Specialisations:    expert.Specialisations,
            YearsExperience:    expert.YearsExperience,
            Credentials:        expert.Credentials,
            LocationCountry:    expert.LocationCountry,
            IsActive:           expert.IsActive,
            CreatedAt:          expert.CreatedAt);
    }
}
