using MediatR;

namespace FemVed.Application.Admin.Commands.CreateExpert;

/// <summary>
/// Creates an expert profile for an existing user account. Admin only.
/// The linked user should already exist in the system (created via registration or by the admin).
/// </summary>
/// <param name="UserId">ID of the existing user to link as an expert.</param>
/// <param name="DisplayName">Public display name, e.g. "Dr. Prathima Nagesh".</param>
/// <param name="Title">Professional title, e.g. "Ayurvedic Physician".</param>
/// <param name="Bio">Full biography displayed on the program detail page.</param>
/// <param name="GridDescription">Short bio for program grid cards (max 500 chars, optional).</param>
/// <param name="DetailedDescription">Detailed long-form description (optional).</param>
/// <param name="ProfileImageUrl">Profile photo URL on Cloudflare R2 (optional).</param>
/// <param name="GridImageUrl">Grid card image URL (optional).</param>
/// <param name="Specialisations">Areas of specialisation, e.g. ["Hormonal Health", "PCOS"] (optional).</param>
/// <param name="YearsExperience">Years of clinical/professional experience (optional).</param>
/// <param name="Credentials">Degrees and certifications, e.g. ["BAMS", "MD Ayurveda"] (optional).</param>
/// <param name="LocationCountry">Country where the expert is based (optional).</param>
/// <param name="AdminUserId">Admin performing the action — written to the audit log.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record CreateExpertCommand(
    Guid UserId,
    string DisplayName,
    string Title,
    string Bio,
    string? GridDescription,
    string? DetailedDescription,
    string? ProfileImageUrl,
    string? GridImageUrl,
    List<string>? Specialisations,
    short? YearsExperience,
    List<string>? Credentials,
    string? LocationCountry,
    Guid AdminUserId,
    string? IpAddress) : IRequest<Guid>;
