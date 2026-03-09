using MediatR;

namespace FemVed.Application.Admin.Commands.UpsertExpertByUserId;

/// <summary>
/// Creates or updates the expert profile for a given user account. Admin only.
/// Used after promoting a user to the Expert role — the role change auto-creates a
/// minimal profile; this command enriches it with admin-supplied details.
/// All profile fields are optional — only non-null values are applied on update.
/// If no expert profile exists yet (edge case), one is created with the supplied fields.
/// </summary>
/// <param name="UserId">The user whose expert profile will be upserted.</param>
/// <param name="DisplayName">Public display name (optional).</param>
/// <param name="Title">Professional title (optional).</param>
/// <param name="Bio">Full biography (optional).</param>
/// <param name="GridDescription">Short bio for grid cards — max 500 chars (optional).</param>
/// <param name="DetailedDescription">Detailed long-form description (optional).</param>
/// <param name="ProfileImageUrl">Profile photo URL (optional).</param>
/// <param name="GridImageUrl">Grid card image URL (optional).</param>
/// <param name="Specialisations">Areas of specialisation (optional).</param>
/// <param name="YearsExperience">Years of clinical experience (optional).</param>
/// <param name="Credentials">Degrees and certifications (optional).</param>
/// <param name="LocationCountry">Country where the expert is based (optional).</param>
/// <param name="AdminUserId">Admin performing the action — written to the audit log.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record UpsertExpertByUserIdCommand(
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
    string? LocationCountry,
    Guid AdminUserId,
    string? IpAddress) : IRequest;
