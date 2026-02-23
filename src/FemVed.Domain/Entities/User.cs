namespace FemVed.Domain.Entities;

/// <summary>
/// All platform users — Admins, Experts, and regular Users.
/// Dual country storage: dial code for display, ISO code for business logic.
/// Soft-deletable: never hard-deleted.
/// </summary>
public class User
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Unique email address used for login.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hashed password (work factor 12). Never logged or returned in responses.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>FK to roles table.</summary>
    public short RoleId { get; set; }

    /// <summary>User's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>User's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Telephone dial code as supplied by the user at registration, e.g. "+91", "+44".</summary>
    public string? CountryDialCode { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code derived from dial code, e.g. "IN", "GB". Drives payment gateway selection.</summary>
    public string? CountryIsoCode { get; set; }

    /// <summary>Mobile number digits only, without dial code.</summary>
    public string? MobileNumber { get; set; }

    /// <summary>Full international mobile: dial code + digits, e.g. "+917890001234".</summary>
    public string? FullMobile { get; set; }

    /// <summary>Whether the mobile has been OTP-verified (not used at MVP).</summary>
    public bool IsMobileVerified { get; set; }

    /// <summary>Whether the email has been verified. Stored but not enforced for purchase at MVP.</summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>User has opted in to receive WhatsApp notifications.</summary>
    public bool WhatsAppOptIn { get; set; }

    /// <summary>Soft-disable: user cannot log in when false.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft-delete flag. Never hard-delete users.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp of record creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC timestamp of last update.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The role assigned to this user.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>Active and historical refresh tokens for JWT rotation.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>Password reset requests for this user.</summary>
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    /// <summary>Expert profile if this user is an Expert (role_id = 2).</summary>
    public Expert? ExpertProfile { get; set; }
}
