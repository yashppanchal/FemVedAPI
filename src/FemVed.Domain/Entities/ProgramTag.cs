namespace FemVed.Domain.Entities;

/// <summary>A filter tag associated with a program, e.g. "stress", "pcos", "gut-health".</summary>
public class ProgramTag
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the program.</summary>
    public Guid ProgramId { get; set; }

    /// <summary>Tag value, lowercase slugified, e.g. "hormones".</summary>
    public string Tag { get; set; } = string.Empty;

    // Navigation
    /// <summary>The program this tag is associated with.</summary>
    public Program Program { get; set; } = null!;
}
