namespace FemVed.Application.Admin.DTOs;

/// <summary>Admin view of an expert profile.</summary>
/// <param name="ExpertId">Expert UUID.</param>
/// <param name="UserId">Linked user account UUID.</param>
/// <param name="UserEmail">Expert's login email (from linked User).</param>
/// <param name="DisplayName">Public display name.</param>
/// <param name="Title">Professional title.</param>
/// <param name="LocationCountry">Country where the expert is based.</param>
/// <param name="CommissionRate">Expert revenue share as a percentage, e.g. 80.00 means expert earns 80%.</param>
/// <param name="IsActive">Whether the expert is visible in the catalog.</param>
/// <param name="IsDeleted">Whether the expert profile has been soft-deleted.</param>
/// <param name="CreatedAt">UTC profile creation timestamp.</param>
/// <param name="Bio">Expert's professional bio.</param>
/// <param name="GridDescription">Short description for grid cards (max 500 chars).</param>
/// <param name="DetailedDescription">Detailed bio for the program detail page.</param>
/// <param name="ProfileImageUrl">Profile image URL.</param>
/// <param name="GridImageUrl">Grid/card image URL.</param>
/// <param name="Specialisations">List of specialisation areas.</param>
/// <param name="Credentials">Professional credentials.</param>
/// <param name="YearsExperience">Years of professional experience.</param>
public record AdminExpertDto(
    Guid ExpertId,
    Guid UserId,
    string UserEmail,
    string DisplayName,
    string Title,
    string? LocationCountry,
    decimal CommissionRate,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    string? Bio,
    string? GridDescription,
    string? DetailedDescription,
    string? ProfileImageUrl,
    string? GridImageUrl,
    string[]? Specialisations,
    string[]? Credentials,
    short? YearsExperience);
