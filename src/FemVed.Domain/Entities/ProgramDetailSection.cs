namespace FemVed.Domain.Entities;

/// <summary>
/// A heading + description section displayed on the program detail page.
/// Ordered by <see cref="SortOrder"/>.
/// Cascade-deleted when the parent <see cref="Program"/> is deleted.
/// </summary>
public class ProgramDetailSection
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent program.</summary>
    public Guid ProgramId { get; set; }

    /// <summary>Section heading, e.g. "Reset Stress Patterns, Restore Hormonal Balance".</summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>Section body text shown beneath the heading.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    // Navigation
    /// <summary>The program this section belongs to.</summary>
    public Program Program { get; set; } = null!;
}
