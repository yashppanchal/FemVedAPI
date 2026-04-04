using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetLibraryCategoryBySlug;

/// <summary>
/// Returns a single library category with its published videos by URL slug.
/// Used to render individual category pages on the frontend.
/// </summary>
/// <param name="Slug">URL slug, e.g. "hormonal-health-support".</param>
/// <param name="LocationCode">ISO country code for price formatting.</param>
public record GetLibraryCategoryBySlugQuery(string Slug, string LocationCode) : IRequest<LibraryCategoryDto>;
