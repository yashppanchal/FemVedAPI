namespace FemVed.Application.Admin.DTOs;

/// <summary>Admin view of an expert profile.</summary>
/// <param name="ExpertId">Expert UUID.</param>
/// <param name="UserId">Linked user account UUID.</param>
/// <param name="UserEmail">Expert's login email (from linked User).</param>
/// <param name="DisplayName">Public display name.</param>
/// <param name="Title">Professional title.</param>
/// <param name="LocationCountry">Country where the expert is based.</param>
/// <param name="IsActive">Whether the expert is visible in the catalog.</param>
/// <param name="IsDeleted">Whether the expert profile has been soft-deleted.</param>
/// <param name="CreatedAt">UTC profile creation timestamp.</param>
public record AdminExpertDto(
    Guid ExpertId,
    Guid UserId,
    string UserEmail,
    string DisplayName,
    string Title,
    string? LocationCountry,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt);
