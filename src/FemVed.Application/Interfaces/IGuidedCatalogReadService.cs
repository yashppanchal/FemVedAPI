using FemVed.Application.Guided.DTOs;

namespace FemVed.Application.Interfaces;

/// <summary>
/// Read-service for complex guided catalog queries that require EF Core projections
/// across multiple joined tables. Defined here in Application; implemented in Infrastructure.
/// This avoids the Application layer taking a direct dependency on EF Core.
/// </summary>
public interface IGuidedCatalogReadService
{
    /// <summary>
    /// Returns the full guided catalog tree (domains → categories → programs) in the
    /// exact shape required by the React frontend.
    /// Only PUBLISHED, non-deleted, active programs are included.
    /// Prices are resolved for the specified location code; falls back to GB pricing.
    /// </summary>
    /// <param name="locationCode">ISO country code driving price selection, e.g. "IN", "GB", "US".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full tree response.</returns>
    Task<GuidedTreeResponse> GetGuidedTreeAsync(string locationCode, CancellationToken cancellationToken = default);

    /// <summary>Returns a single category with its programs by URL slug.</summary>
    /// <param name="slug">Category URL slug.</param>
    /// <param name="locationCode">ISO country code for price formatting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category DTO, or null if not found / inactive.</returns>
    Task<GuidedCategoryDto?> GetCategoryBySlugAsync(string slug, string locationCode, CancellationToken cancellationToken = default);

    /// <summary>Returns a single program with full detail by URL slug.</summary>
    /// <param name="slug">Program URL slug.</param>
    /// <param name="locationCode">ISO country code for price formatting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Program DTO, or null if not found / not published.</returns>
    Task<ProgramInCategoryDto?> GetProgramBySlugAsync(string slug, string locationCode, CancellationToken cancellationToken = default);
}
