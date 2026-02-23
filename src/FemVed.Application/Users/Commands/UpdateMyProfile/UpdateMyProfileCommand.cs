using FemVed.Application.Users.DTOs;
using MediatR;

namespace FemVed.Application.Users.Commands.UpdateMyProfile;

/// <summary>
/// Updates the authenticated user's editable profile fields.
/// Email is not editable via this command.
/// Country code and mobile number must be supplied together or both omitted.
/// </summary>
/// <param name="UserId">The authenticated user's ID (injected from JWT by the controller).</param>
/// <param name="FirstName">Updated first name.</param>
/// <param name="LastName">Updated last name.</param>
/// <param name="CountryCode">Optional dial code, e.g. "+91".</param>
/// <param name="MobileNumber">Optional mobile digits only (no dial code).</param>
/// <param name="WhatsAppOptIn">Whether the user opts in to WhatsApp notifications.</param>
public record UpdateMyProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? CountryCode,
    string? MobileNumber,
    bool WhatsAppOptIn) : IRequest<UserProfileDto>;
