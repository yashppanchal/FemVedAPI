namespace FemVed.Domain.Entities;

/// <summary>
/// "What's included" bullet point shown on the purchase card for a library video.
/// Example: "▶ 30 episodes, lifetime access" or "♾ Lifetime access, watch anytime".
/// </summary>
public class LibraryVideoFeature
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the video.</summary>
    public Guid VideoId { get; set; }

    /// <summary>Icon character, e.g. "▶", "♾", "📱", "📄", "✦".</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Feature description, e.g. "Lifetime access, watch anytime".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    // Navigation
    /// <summary>The video this feature belongs to.</summary>
    public LibraryVideo Video { get; set; } = null!;
}
