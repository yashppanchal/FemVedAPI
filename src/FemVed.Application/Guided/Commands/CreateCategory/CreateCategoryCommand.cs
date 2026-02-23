using MediatR;

namespace FemVed.Application.Guided.Commands.CreateCategory;

/// <summary>
/// Creates a new guided category within a domain.
/// AdminOnly operation.
/// </summary>
/// <param name="DomainId">The domain this category belongs to.</param>
/// <param name="Name">Display name, e.g. "Hormonal Health Support".</param>
/// <param name="Slug">Unique URL slug, e.g. "hormonal-health-support".</param>
/// <param name="CategoryType">Short type label used in card display.</param>
/// <param name="HeroTitle">Main headline on the category page.</param>
/// <param name="HeroSubtext">Supporting copy below the headline.</param>
/// <param name="CtaLabel">Call-to-action button label.</param>
/// <param name="CtaLink">Call-to-action link target.</param>
/// <param name="PageHeader">Section header above the program grid.</param>
/// <param name="ImageUrl">Category hero image URL.</param>
/// <param name="SortOrder">Display ordering within the domain.</param>
/// <param name="ParentId">Parent category ID for subcategories; null for top-level.</param>
/// <param name="WhatsIncluded">Bullet-point texts for the hero "What's Included" section.</param>
/// <param name="KeyAreas">Key support area texts listed on the category page.</param>
public record CreateCategoryCommand(
    Guid DomainId,
    string Name,
    string Slug,
    string CategoryType,
    string HeroTitle,
    string HeroSubtext,
    string? CtaLabel,
    string? CtaLink,
    string? PageHeader,
    string? ImageUrl,
    int SortOrder,
    Guid? ParentId,
    List<string> WhatsIncluded,
    List<string> KeyAreas) : IRequest<Guid>;
