namespace FemVed.Domain.Entities;

/// <summary>A duration option for a program, e.g. "6 weeks" (6). Each duration has location-specific pricing.</summary>
public class ProgramDuration
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent program.</summary>
    public Guid ProgramId { get; set; }

    /// <summary>Human-readable label, e.g. "6 weeks".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Number of weeks for this duration option.</summary>
    public short Weeks { get; set; }

    /// <summary>Whether this duration is available for purchase.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The program this duration belongs to.</summary>
    public Program Program { get; set; } = null!;

    /// <summary>Location-specific prices for this duration (IN/GB/US).</summary>
    public ICollection<DurationPrice> Prices { get; set; } = new List<DurationPrice>();
}
