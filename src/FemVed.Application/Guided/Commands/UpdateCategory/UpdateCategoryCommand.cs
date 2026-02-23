using MediatR;

namespace FemVed.Application.Guided.Commands.UpdateCategory;

/// <summary>
/// Updates an existing category's content.
/// AdminOnly operation. All fields are optional — only non-null values are applied.
/// </summary>
/// <param name="CategoryId">The category to update.</param>
/// <param name="Name">New display name (optional).</param>
/// <param name="CategoryType">New category type label (optional).</param>
/// <param name="HeroTitle">New hero headline (optional).</param>
/// <param name="HeroSubtext">New hero subtext (optional).</param>
/// <param name="CtaLabel">New CTA label (optional).</param>
/// <param name="CtaLink">New CTA link (optional).</param>
/// <param name="PageHeader">New page header (optional).</param>
/// <param name="ImageUrl">New image URL (optional).</param>
/// <param name="SortOrder">New sort order (optional).</param>
/// <param name="WhatsIncluded">Replaces all existing WhatsIncluded items (optional).</param>
/// <param name="KeyAreas">Replaces all existing KeyArea items (optional).</param>
public record UpdateCategoryCommand(
    Guid CategoryId,
    string? Name,
    string? CategoryType,
    string? HeroTitle,
    string? HeroSubtext,
    string? CtaLabel,
    string? CtaLink,
    string? PageHeader,
    string? ImageUrl,
    int? SortOrder,
    List<string>? WhatsIncluded,
    List<string>? KeyAreas) : IRequest;
