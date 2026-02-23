namespace FemVed.Domain.Entities;

/// <summary>A user review or testimonial displayed on a program detail page and optionally the homepage.</summary>
public class ProgramTestimonial
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the reviewed program.</summary>
    public Guid ProgramId { get; set; }

    /// <summary>Reviewer's display name.</summary>
    public string ReviewerName { get; set; } = string.Empty;

    /// <summary>Reviewer context, e.g. "Mother of two, London".</summary>
    public string? ReviewerTitle { get; set; }

    /// <summary>The testimonial body text.</summary>
    public string ReviewText { get; set; } = string.Empty;

    /// <summary>Star rating 1–5. Nullable when not using a star rating system.</summary>
    public short? Rating { get; set; }

    /// <summary>Whether this testimonial is shown publicly.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    /// <summary>The program this testimonial is for.</summary>
    public Program Program { get; set; } = null!;
}
