namespace FemVed.Application.Guided.DTOs;

/// <summary>
/// Represents a single testimonial record as returned by the management endpoints.
/// Includes IsActive so admins and experts can see hidden testimonials too.
/// </summary>
/// <param name="TestimonialId">UUID of the testimonial record.</param>
/// <param name="ProgramId">UUID of the program this testimonial belongs to.</param>
/// <param name="ReviewerName">Reviewer's display name.</param>
/// <param name="ReviewerTitle">Reviewer context, e.g. "Mother of two, London". Null if not provided.</param>
/// <param name="ReviewText">The testimonial body text.</param>
/// <param name="Rating">Star rating 1–5. Null when no star rating is used.</param>
/// <param name="IsActive">Whether this testimonial is shown publicly on the catalog.</param>
/// <param name="SortOrder">Display ordering (ascending).</param>
/// <param name="CreatedAt">UTC timestamp when the record was created.</param>
public record TestimonialDto(
    Guid TestimonialId,
    Guid ProgramId,
    string ReviewerName,
    string? ReviewerTitle,
    string ReviewText,
    short? Rating,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAt);
