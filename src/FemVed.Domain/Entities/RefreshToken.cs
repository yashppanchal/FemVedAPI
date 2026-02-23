namespace FemVed.Domain.Entities;

/// <summary>
/// JWT refresh token record. Rotation policy: old token is revoked immediately when a new one is issued.
/// Token value is stored as a BCrypt hash — never the raw token.
/// </summary>
public class RefreshToken
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to users.</summary>
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the raw refresh token. The raw value is sent to the client only once.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>UTC expiry. Tokens must not be accepted after this time.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>True if this token has been used and superseded by a new rotation, or explicitly logged out.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>UTC timestamp of record creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    /// <summary>The user who owns this token.</summary>
    public User User { get; set; } = null!;
}
