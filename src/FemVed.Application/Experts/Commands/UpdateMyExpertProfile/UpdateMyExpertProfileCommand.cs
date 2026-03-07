using FemVed.Application.Experts.DTOs;
using MediatR;

namespace FemVed.Application.Experts.Commands.UpdateMyExpertProfile;

/// <summary>
/// Updates the authenticated expert's own profile fields.
/// All payload fields are optional (patch semantics); only non-null values are applied.
/// CommissionRate and IsActive are intentionally excluded — those are admin-only fields.
/// </summary>
/// <param name="UserId">The authenticated user's ID (from JWT). Used to resolve the expert profile.</param>
/// <param name="DisplayName">New public display name. Null = no change.</param>
/// <param name="Title">New professional title. Null = no change.</param>
/// <param name="Bio">New full biography text. Null = no change.</param>
/// <param name="GridDescription">New short bio for grid cards (max 500 chars). Null = no change.</param>
/// <param name="DetailedDescription">New detailed bio for program detail page. Null = no change.</param>
/// <param name="ProfileImageUrl">New profile photo URL. Null = no change.</param>
/// <param name="GridImageUrl">New grid card image URL. Null = no change.</param>
/// <param name="Specialisations">New list of specialisation areas. Null = no change.</param>
/// <param name="YearsExperience">New years of experience. Null = no change.</param>
/// <param name="Credentials">New list of credentials/certifications. Null = no change.</param>
/// <param name="LocationCountry">New country. Null = no change.</param>
public record UpdateMyExpertProfileCommand(
    Guid UserId,
    string? DisplayName,
    string? Title,
    string? Bio,
    string? GridDescription,
    string? DetailedDescription,
    string? ProfileImageUrl,
    string? GridImageUrl,
    List<string>? Specialisations,
    short? YearsExperience,
    List<string>? Credentials,
    string? LocationCountry) : IRequest<ExpertProfileDto>;
