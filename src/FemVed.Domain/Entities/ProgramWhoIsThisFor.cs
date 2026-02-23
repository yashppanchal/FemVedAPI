namespace FemVed.Domain.Entities;

/// <summary>A single target-audience bullet point in the "Who Is This For" section of a program page.</summary>
public class ProgramWhoIsThisFor
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent program.</summary>
    public Guid ProgramId { get; set; }

    /// <summary>Audience descriptor text.</summary>
    public string ItemText { get; set; } = string.Empty;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    // Navigation
    /// <summary>The program this item belongs to.</summary>
    public Program Program { get; set; } = null!;
}
