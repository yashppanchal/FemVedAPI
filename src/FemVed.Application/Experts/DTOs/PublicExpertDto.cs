namespace FemVed.Application.Experts.DTOs;

/// <summary>
/// Public-catalog representation of an expert. Returned by the anonymous
/// <c>GET /api/v1/experts</c> endpoint. Field shape matches the published contract.
/// </summary>
/// <param name="UserEmail">Email of the user account linked to this expert profile.</param>
/// <param name="DisplayName">Public display name, e.g. "Dr. Prathima Nagesh".</param>
/// <param name="Title">Professional title.</param>
/// <param name="LocationCountry">Country where the expert is based, or null.</param>
/// <param name="IsActive">Whether the profile is visible in the catalog.</param>
/// <param name="IsDeleted">Soft-delete flag.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="Bio">Full biography.</param>
/// <param name="GridDescription">Short bio shown on grid cards (max 500 chars), or null.</param>
/// <param name="DetailedDescription">Detailed bio shown on the program detail page, or null.</param>
/// <param name="ProfileImageUrl">Profile photo URL, or null.</param>
/// <param name="GridImageUrl">Grid card image URL, or null.</param>
/// <param name="Specialisations">Areas of specialisation, or null.</param>
/// <param name="Credentials">Degrees and certifications, or null.</param>
/// <param name="YearsExperience">Years of professional experience, or null.</param>
public sealed record PublicExpertDto(
    string UserEmail,
    string DisplayName,
    string Title,
    string? LocationCountry,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    string Bio,
    string? GridDescription,
    string? DetailedDescription,
    string? ProfileImageUrl,
    string? GridImageUrl,
    string[]? Specialisations,
    string[]? Credentials,
    short? YearsExperience);
