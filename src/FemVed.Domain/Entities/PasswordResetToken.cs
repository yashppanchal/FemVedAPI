namespace FemVed.Domain.Entities;

/// <summary>
/// Time-limited password reset token sent via email.
/// Token is hashed before storage.
/// </summary>
public class PasswordResetToken
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to users.</summary>
    public Guid UserId { get; set; }

    /// <summary>Hash of the raw token URL parameter.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>UTC expiry — tokens are typically valid for 1 hour.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>True once the token has been used to reset a password. Prevents replay.</summary>
    public bool IsUsed { get; set; }

    /// <summary>UTC timestamp of record creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    /// <summary>The user who requested the password reset.</summary>
    public User User { get; set; } = null!;
}
