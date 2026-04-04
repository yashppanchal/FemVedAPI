namespace FemVed.Domain.Entities;

/// <summary>
/// Tag displayed on a library video detail page and used for search.
/// </summary>
public class LibraryVideoTag
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the video.</summary>
    public Guid VideoId { get; set; }

    /// <summary>Tag text, e.g. "Hormones", "Cycle", "Nutrition".</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    // Navigation
    /// <summary>The video this tag belongs to.</summary>
    public LibraryVideo Video { get; set; } = null!;
}
