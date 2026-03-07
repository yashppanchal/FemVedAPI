using FemVed.Application.Experts.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Commands.UpdateMyExpertProfile;

/// <summary>
/// Handles <see cref="UpdateMyExpertProfileCommand"/>.
/// Applies non-null patches to the authenticated expert's profile.
/// CommissionRate and IsActive can only be changed by an Admin.
/// </summary>
public sealed class UpdateMyExpertProfileCommandHandler
    : IRequestHandler<UpdateMyExpertProfileCommand, ExpertProfileDto>
{
    private readonly IRepository<Expert> _experts;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateMyExpertProfileCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public UpdateMyExpertProfileCommandHandler(
        IRepository<Expert> experts,
        IUnitOfWork uow,
        ILogger<UpdateMyExpertProfileCommandHandler> logger)
    {
        _experts = experts;
        _uow     = uow;
        _logger  = logger;
    }

    /// <summary>Updates the expert's own profile and returns the updated DTO.</summary>
    /// <exception cref="NotFoundException">Thrown when no expert profile is linked to the user.</exception>
    public async Task<ExpertProfileDto> Handle(
        UpdateMyExpertProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateMyExpertProfile: user {UserId} updating own expert profile", request.UserId);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.UserId == request.UserId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Expert profile", request.UserId);

        if (request.DisplayName is not null)        expert.DisplayName         = request.DisplayName.Trim();
        if (request.Title is not null)              expert.Title               = request.Title.Trim();
        if (request.Bio is not null)                expert.Bio                 = request.Bio.Trim();
        if (request.GridDescription is not null)    expert.GridDescription     = request.GridDescription.Trim();
        if (request.DetailedDescription is not null)expert.DetailedDescription = request.DetailedDescription.Trim();
        if (request.ProfileImageUrl is not null)    expert.ProfileImageUrl     = request.ProfileImageUrl.Trim();
        if (request.GridImageUrl is not null)       expert.GridImageUrl        = request.GridImageUrl.Trim();
        if (request.Specialisations is not null)    expert.Specialisations     = request.Specialisations.ToArray();
        if (request.YearsExperience.HasValue)       expert.YearsExperience     = request.YearsExperience.Value;
        if (request.Credentials is not null)        expert.Credentials         = request.Credentials.ToArray();
        if (request.LocationCountry is not null)    expert.LocationCountry     = request.LocationCountry.Trim();
        expert.UpdatedAt = DateTimeOffset.UtcNow;

        _experts.Update(expert);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateMyExpertProfile: expert {ExpertId} profile updated", expert.Id);

        return new ExpertProfileDto(
            ExpertId:           expert.Id,
            UserId:             expert.UserId,
            DisplayName:        expert.DisplayName,
            Title:              expert.Title,
            Bio:                expert.Bio,
            GridDescription:    expert.GridDescription,
            DetailedDescription:expert.DetailedDescription,
            ProfileImageUrl:    expert.ProfileImageUrl,
            GridImageUrl:       expert.GridImageUrl,
            Specialisations:    expert.Specialisations,
            YearsExperience:    expert.YearsExperience,
            Credentials:        expert.Credentials,
            LocationCountry:    expert.LocationCountry,
            CommissionRate:     expert.CommissionRate,
            IsActive:           expert.IsActive,
            CreatedAt:          expert.CreatedAt);
    }
}
