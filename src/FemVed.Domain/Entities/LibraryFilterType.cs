using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>
/// Dynamic filter tab for the Wellness Library catalog page.
/// Admin-managed — content team adds/removes/reorders filters without code changes.
/// </summary>
public class LibraryFilterType
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent library domain.</summary>
    public Guid DomainId { get; set; }

    /// <summary>Display name, e.g. "Masterclasses", "Mindfulness".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Query param key, e.g. "masterclass", "mindfulness".</summary>
    public string FilterKey { get; set; } = string.Empty;

    /// <summary>What this filter matches against: VIDEO_TYPE or CATEGORY.</summary>
    public FilterTarget FilterTarget { get; set; }

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this filter tab is visible.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    /// <summary>The parent domain.</summary>
    public LibraryDomain Domain { get; set; } = null!;
}
