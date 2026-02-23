namespace FemVed.Application.Users.DTOs;

/// <summary>
/// Response shape for GET /api/v1/users/me and PUT /api/v1/users/me.
/// </summary>
/// <param name="UserId">User UUID.</param>
/// <param name="Email">Login email address (read-only — cannot be changed via profile update).</param>
/// <param name="FirstName">User's first name.</param>
/// <param name="LastName">User's last name.</param>
/// <param name="CountryCode">Telephone dial code, e.g. "+91".</param>
/// <param name="CountryIsoCode">ISO 3166-1 alpha-2 code derived from dial code, e.g. "IN".</param>
/// <param name="MobileNumber">Digits-only mobile number (no dial code).</param>
/// <param name="FullMobile">Concatenated international number, e.g. "+917726262623".</param>
/// <param name="WhatsAppOptIn">Whether the user receives WhatsApp notifications.</param>
/// <param name="IsEmailVerified">Whether the email address has been verified.</param>
/// <param name="CreatedAt">UTC timestamp when the account was created.</param>
public record UserProfileDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? CountryCode,
    string? CountryIsoCode,
    string? MobileNumber,
    string? FullMobile,
    bool WhatsAppOptIn,
    bool IsEmailVerified,
    DateTimeOffset CreatedAt);
