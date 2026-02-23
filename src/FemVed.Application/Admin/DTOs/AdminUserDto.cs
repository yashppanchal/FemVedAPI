namespace FemVed.Application.Admin.DTOs;

/// <summary>Admin view of a user account, including activation state and role.</summary>
/// <param name="UserId">User UUID.</param>
/// <param name="Email">Login email.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="RoleId">Numeric role ID (1=Admin, 2=Expert, 3=User).</param>
/// <param name="RoleName">Role display name.</param>
/// <param name="CountryIsoCode">ISO country code, e.g. "GB".</param>
/// <param name="FullMobile">Full international mobile number.</param>
/// <param name="IsEmailVerified">Whether email is verified.</param>
/// <param name="WhatsAppOptIn">Whether user opted into WhatsApp notifications.</param>
/// <param name="IsActive">Whether the user can log in.</param>
/// <param name="IsDeleted">Whether the user has been soft-deleted.</param>
/// <param name="CreatedAt">UTC account creation timestamp.</param>
public record AdminUserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    short RoleId,
    string RoleName,
    string? CountryIsoCode,
    string? FullMobile,
    bool IsEmailVerified,
    bool WhatsAppOptIn,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset CreatedAt);
