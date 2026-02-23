using FemVed.Application.Experts.DTOs;
using MediatR;

namespace FemVed.Application.Experts.Queries.GetMyExpertProfile;

/// <summary>Returns the expert profile linked to the authenticated user.</summary>
/// <param name="UserId">The authenticated user's ID (from JWT). Used to locate the expert profile via UserId FK.</param>
public record GetMyExpertProfileQuery(Guid UserId) : IRequest<ExpertProfileDto>;
