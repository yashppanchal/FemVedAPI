using FemVed.Application.Library.DTOs;

namespace FemVed.Application.Interfaces;

/// <summary>
/// Read-service for complex Wellness Library catalog queries that require EF Core projections
/// across multiple joined tables. Defined here in Application; implemented in Infrastructure.
/// This avoids the Application layer taking a direct dependency on EF Core.
/// </summary>
public interface ILibraryCatalogReadService
{
    /// <summary>
    /// Returns the full library catalog tree (domain → filters → featured → categories → videos)
    /// in the exact shape required by the React frontend.
    /// Only PUBLISHED, non-deleted videos are included.
    /// Prices are resolved for the specified location code; falls back to GB pricing.
    /// </summary>
    /// <param name="locationCode">ISO country code driving price selection, e.g. "IN", "GB", "US".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full tree response.</returns>
    Task<LibraryTreeResponse> GetLibraryTreeAsync(string locationCode, CancellationToken cancellationToken = default);

    /// <summary>Returns a single library category with its published videos by URL slug.</summary>
    /// <param name="slug">Category URL slug.</param>
    /// <param name="locationCode">ISO country code for price formatting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category DTO, or null if not found / inactive.</returns>
    Task<LibraryCategoryDto?> GetCategoryBySlugAsync(string slug, string locationCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single published video's full detail by URL slug.
    /// Stream URLs are NEVER included — only the trailer URL.
    /// </summary>
    /// <param name="slug">Video URL slug.</param>
    /// <param name="locationCode">ISO country code for price formatting.</param>
    /// <param name="currentUserId">Authenticated user's ID, or null for anonymous callers. Used to check purchase status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Video detail DTO, or null if not found / not published.</returns>
    Task<LibraryVideoDetailResponse?> GetVideoBySlugAsync(string slug, string locationCode, Guid? currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Returns all active filter types for the library catalog.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of filter DTOs (including the synthetic "All Programs" entry).</returns>
    Task<List<LibraryFilterDto>> GetFilterTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the authenticated user's purchased library videos with watch progress.
    /// Includes video, expert, episodes, and category data via EF Core projections.
    /// </summary>
    /// <param name="userId">Authenticated user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>My library response with purchased videos.</returns>
    Task<MyLibraryResponse> GetMyLibraryAsync(Guid userId, CancellationToken cancellationToken = default);
}
