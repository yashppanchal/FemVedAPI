using MediatR;

namespace FemVed.Application.Library.Commands.CreateLibraryDomain;

/// <summary>
/// Creates a new library domain (e.g. "Wellness Library").
/// AdminOnly operation.
/// </summary>
/// <param name="Name">Display name shown in UI navigation.</param>
/// <param name="Slug">URL slug -- must be unique, e.g. "wellness-library".</param>
/// <param name="Description">Optional description of this domain.</param>
/// <param name="HeroImageDesktop">Hero banner image URL for desktop (optional).</param>
/// <param name="HeroImageMobile">Hero banner image URL for mobile (optional).</param>
/// <param name="HeroImagePortrait">Hero banner image URL portrait (optional).</param>
/// <param name="SortOrder">Display ordering (ascending).</param>
public record CreateLibraryDomainCommand(
    string Name,
    string Slug,
    string? Description,
    string? HeroImageDesktop,
    string? HeroImageMobile,
    string? HeroImagePortrait,
    int SortOrder) : IRequest<Guid>;
