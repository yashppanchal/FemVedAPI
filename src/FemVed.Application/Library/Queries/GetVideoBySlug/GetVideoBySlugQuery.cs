using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetVideoBySlug;

/// <summary>
/// Returns a single published library video's full detail by URL slug.
/// Stream URLs are NEVER returned — only the public trailer.
/// The <see cref="CurrentUserId"/> is used to check if the caller has purchased this video.
/// </summary>
/// <param name="Slug">Video URL slug, e.g. "cycle-reset-method".</param>
/// <param name="LocationCode">ISO country code for price formatting.</param>
/// <param name="CurrentUserId">Authenticated user's ID, or null for anonymous callers.</param>
public record GetVideoBySlugQuery(string Slug, string LocationCode, Guid? CurrentUserId)
    : IRequest<LibraryVideoDetailResponse>;
