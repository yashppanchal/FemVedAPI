using MediatR;

namespace FemVed.Application.Admin.Commands.UpdateExpert;

/// <summary>
/// Updates an existing expert profile. Admin only.
/// All profile fields are optional — only non-null values are applied.
/// </summary>
/// <param name="ExpertId">The expert profile to update.</param>
/// <param name="DisplayName">New public display name (optional).</param>
/// <param name="Title">New professional title (optional).</param>
/// <param name="Bio">New full biography (optional).</param>
/// <param name="GridDescription">New short bio for grid cards — max 500 chars (optional).</param>
/// <param name="DetailedDescription">New detailed description (optional).</param>
/// <param name="ProfileImageUrl">New profile photo URL (optional).</param>
/// <param name="GridImageUrl">New grid card image URL (optional).</param>
/// <param name="Specialisations">Replaces all specialisations (optional).</param>
/// <param name="YearsExperience">New years of experience (optional).</param>
/// <param name="Credentials">Replaces all credential entries (optional).</param>
/// <param name="LocationCountry">New country (optional).</param>
/// <param name="AdminUserId">Admin performing the action — written to the audit log.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record UpdateExpertCommand(
    Guid ExpertId,
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
    string? LocationCountry,
    Guid AdminUserId,
    string? IpAddress) : IRequest;
