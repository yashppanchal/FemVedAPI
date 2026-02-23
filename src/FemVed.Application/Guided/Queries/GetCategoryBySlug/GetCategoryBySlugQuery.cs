using FemVed.Application.Guided.DTOs;
using MediatR;

namespace FemVed.Application.Guided.Queries.GetCategoryBySlug;

/// <summary>
/// Returns a single category with its published programs by URL slug.
/// Used to render individual category pages on the frontend.
/// </summary>
/// <param name="Slug">URL slug, e.g. "hormonal-health-support".</param>
/// <param name="LocationCode">ISO country code for price formatting.</param>
public record GetCategoryBySlugQuery(string Slug, string LocationCode) : IRequest<GuidedCategoryDto>;
