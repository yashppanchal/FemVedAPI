namespace FemVed.Domain.Entities;

/// <summary>
/// Expert practitioner profile, linked 1:1 to a User account with role Expert.
/// Soft-deletable.
/// </summary>
public class Expert
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the user account that owns this expert profile.</summary>
    public Guid UserId { get; set; }

    /// <summary>Public display name shown in the catalog, e.g. "Dr. Prathima Nagesh".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Professional title, e.g. "Ayurvedic Physician &amp; Women's Health Specialist".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full biography shown on the program detail page.</summary>
    public string Bio { get; set; } = string.Empty;

    /// <summary>Short bio (max 500 chars) used in program grid cards.</summary>
    public string? ShortBio { get; set; }

    /// <summary>Profile photo URL (hosted on Cloudflare R2).</summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>Areas of specialisation, e.g. ["Hormonal Health", "PCOS"].</summary>
    public string[]? Specialisations { get; set; }

    /// <summary>Years of clinical/professional experience.</summary>
    public short? YearsExperience { get; set; }

    /// <summary>Degrees and certifications, e.g. ["BAMS", "MD Ayurveda"].</summary>
    public string[]? Credentials { get; set; }

    /// <summary>Country where the expert is based, e.g. "India".</summary>
    public string? LocationCountry { get; set; }

    /// <summary>Whether this expert is visible in the catalog.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft-delete flag. Never hard-delete expert profiles.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The user account linked to this expert.</summary>
    public User User { get; set; } = null!;

    /// <summary>Programs offered by this expert.</summary>
    public ICollection<Program> Programs { get; set; } = new List<Program>();
}
