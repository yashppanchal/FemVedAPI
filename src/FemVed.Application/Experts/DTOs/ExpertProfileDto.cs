namespace FemVed.Application.Experts.DTOs;

/// <summary>
/// Response shape for GET /api/v1/experts/me.
/// Contains the authenticated expert's public and internal profile fields.
/// </summary>
/// <param name="ExpertId">UUID of the expert record.</param>
/// <param name="UserId">UUID of the linked user account.</param>
/// <param name="DisplayName">Public display name shown in the catalog.</param>
/// <param name="Title">Professional title.</param>
/// <param name="Bio">Full biography shown on program detail pages.</param>
/// <param name="GridDescription">Short bio used in program grid cards (max 500 chars) — <c>expertGridDescription</c> in the guided tree response.</param>
/// <param name="DetailedDescription">Detailed expert description shown on the program detail page — <c>expertDetailedDescription</c> in the guided tree response.</param>
/// <param name="ProfileImageUrl">Profile photo URL (Cloudflare R2).</param>
/// <param name="GridImageUrl">Expert grid card image URL — <c>expertGridImageUrl</c> in the guided tree response.</param>
/// <param name="Specialisations">Areas of specialisation.</param>
/// <param name="YearsExperience">Years of clinical/professional experience.</param>
/// <param name="Credentials">Degrees and certifications.</param>
/// <param name="LocationCountry">Country where the expert is based.</param>
/// <param name="IsActive">Whether the expert is visible in the catalog.</param>
/// <param name="CreatedAt">UTC account creation timestamp.</param>
public record ExpertProfileDto(
    Guid ExpertId,
    Guid UserId,
    string DisplayName,
    string Title,
    string Bio,
    string? GridDescription,
    string? DetailedDescription,
    string? ProfileImageUrl,
    string? GridImageUrl,
    string[]? Specialisations,
    short? YearsExperience,
    string[]? Credentials,
    string? LocationCountry,
    bool IsActive,
    DateTimeOffset CreatedAt);
