using MediatR;

namespace FemVed.Application.Library.Commands.UpdateLibraryDomain;

/// <summary>
/// Updates an existing library domain. All fields except DomainId are optional --
/// only non-null values are applied.
/// AdminOnly operation.
/// </summary>
/// <param name="DomainId">The domain to update.</param>
/// <param name="Name">New display name (optional).</param>
/// <param name="Slug">New URL slug (optional). Must be unique.</param>
/// <param name="Description">New description (optional).</param>
/// <param name="HeroImageDesktop">New desktop hero image URL (optional).</param>
/// <param name="HeroImageMobile">New mobile hero image URL (optional).</param>
/// <param name="HeroImagePortrait">New portrait hero image URL (optional).</param>
/// <param name="SortOrder">New display order (optional).</param>
/// <param name="IsActive">New active status (optional).</param>
public record UpdateLibraryDomainCommand(
    Guid DomainId,
    string? Name,
    string? Slug,
    string? Description,
    string? HeroImageDesktop,
    string? HeroImageMobile,
    string? HeroImagePortrait,
    int? SortOrder,
    bool? IsActive) : IRequest;
