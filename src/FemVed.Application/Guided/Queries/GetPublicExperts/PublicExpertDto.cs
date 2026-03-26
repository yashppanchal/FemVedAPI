namespace FemVed.Application.Guided.Queries.GetPublicExperts;

/// <summary>
/// Lightweight expert card data for the public homepage / experts listing.
/// Excludes sensitive fields (commission, user ID, etc.).
/// </summary>
public sealed record PublicExpertDto(
    /// <summary>Expert ID (stable, safe to expose).</summary>
    Guid ExpertId,
    /// <summary>Public display name, e.g. "Dr. Prathima Nagesh".</summary>
    string DisplayName,
    /// <summary>Professional title, e.g. "Ayurvedic Physician".</summary>
    string Title,
    /// <summary>Short description for grid cards (max 500 chars).</summary>
    string? GridDescription,
    /// <summary>Profile photo URL.</summary>
    string? ProfileImageUrl,
    /// <summary>Grid card image URL.</summary>
    string? GridImageUrl,
    /// <summary>Areas of expertise, e.g. ["Hormonal Health", "PCOS"].</summary>
    string[]? Specialisations,
    /// <summary>Years of clinical experience.</summary>
    short? YearsExperience,
    /// <summary>Country where the expert is based.</summary>
    string? LocationCountry,
    /// <summary>Number of published programs this expert offers.</summary>
    int PublishedProgramCount);
