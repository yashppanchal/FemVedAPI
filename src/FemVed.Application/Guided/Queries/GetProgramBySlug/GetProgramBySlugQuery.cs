using FemVed.Application.Guided.DTOs;
using MediatR;

namespace FemVed.Application.Guided.Queries.GetProgramBySlug;

/// <summary>
/// Returns a single published program with full detail page content by URL slug.
/// </summary>
/// <param name="Slug">Program URL slug, e.g. "break-stress-hormone-health-triangle".</param>
/// <param name="LocationCode">ISO country code for price formatting.</param>
public record GetProgramBySlugQuery(string Slug, string LocationCode) : IRequest<ProgramInCategoryDto>;
