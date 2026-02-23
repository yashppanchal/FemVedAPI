namespace FemVed.Domain.Entities;

/// <summary>A single bullet point in the "What You Get" section of a program detail page.</summary>
public class ProgramWhatYouGet
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent program.</summary>
    public Guid ProgramId { get; set; }

    /// <summary>Benefit description text.</summary>
    public string ItemText { get; set; } = string.Empty;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    // Navigation
    /// <summary>The program this item belongs to.</summary>
    public Program Program { get; set; } = null!;
}
