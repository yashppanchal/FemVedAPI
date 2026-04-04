using System.Security.Claims;
using FemVed.Application.Library.Commands.AddLibraryEpisode;
using FemVed.Application.Library.Commands.AddLibraryTestimonial;
using FemVed.Application.Library.Commands.AddLibraryTierPrice;
using FemVed.Application.Library.Commands.AddLibraryVideoPrice;
using FemVed.Application.Library.Commands.ArchiveLibraryVideo;
using FemVed.Application.Library.Commands.CreateLibraryCategory;
using FemVed.Application.Library.Commands.CreateLibraryDomain;
using FemVed.Application.Library.Commands.CreateLibraryFilterType;
using FemVed.Application.Library.Commands.CreateLibraryVideo;
using FemVed.Application.Library.Commands.DeleteLibraryCategory;
using FemVed.Application.Library.Commands.DeleteLibraryDomain;
using FemVed.Application.Library.Commands.DeleteLibraryEpisode;
using FemVed.Application.Library.Commands.DeleteLibraryFilterType;
using FemVed.Application.Library.Commands.DeleteLibraryTestimonial;
using FemVed.Application.Library.Commands.DeleteLibraryTierPrice;
using FemVed.Application.Library.Commands.DeleteLibraryVideo;
using FemVed.Application.Library.Commands.DeleteLibraryVideoPrice;
using FemVed.Application.Library.Commands.PublishLibraryVideo;
using FemVed.Application.Library.Commands.RejectLibraryVideo;
using FemVed.Application.Library.Commands.SubmitLibraryVideoForReview;
using FemVed.Application.Library.Commands.UpdateLibraryCategory;
using FemVed.Application.Library.Commands.UpdateLibraryDomain;
using FemVed.Application.Library.Commands.UpdateLibraryEpisode;
using FemVed.Application.Library.Commands.UpdateLibraryFilterType;
using FemVed.Application.Library.Commands.UpdateLibraryTestimonial;
using FemVed.Application.Library.Commands.UpdateLibraryTierPrice;
using FemVed.Application.Library.Commands.UpdateLibraryVideo;
using FemVed.Application.Library.Commands.UpdateLibraryVideoPrice;
using FemVed.Application.Library.DTOs;
using FemVed.Application.Library.Queries.GetAllLibraryCategories;
using FemVed.Application.Library.Queries.GetAllLibraryDomains;
using FemVed.Application.Library.Queries.GetAllLibraryFilterTypes;
using FemVed.Application.Library.Queries.GetAllLibraryPriceTiers;
using FemVed.Application.Library.Queries.GetAllLibraryVideos;
using FemVed.Application.Library.Queries.GetLibraryVideoEditDetails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Admin-only CRUD endpoints for the Wellness Library module.
/// Base route: /api/v1/admin/library
/// </summary>
[ApiController]
[Route("api/v1/admin/library")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminLibraryController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    public AdminLibraryController(IMediator mediator) => _mediator = mediator;

    // ── Domains ───────────────────────────────────────────────────────────────

    /// <summary>Returns all library domains.</summary>
    [HttpGet("domains")]
    [ProducesResponseType(typeof(List<AdminLibraryDomainDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDomains(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAllLibraryDomainsQuery(), ct));

    /// <summary>Creates a new library domain.</summary>
    [HttpPost("domains")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDomain([FromBody] CreateLibraryDomainRequest r, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateLibraryDomainCommand(
            r.Name, r.Slug, r.Description, r.HeroImageDesktop, r.HeroImageMobile, r.HeroImagePortrait, r.SortOrder), ct);
        return Created($"/api/v1/admin/library/domains/{id}", id);
    }

    /// <summary>Updates an existing library domain.</summary>
    [HttpPut("domains/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDomain(Guid id, [FromBody] UpdateLibraryDomainRequest r, CancellationToken ct)
    {
        await _mediator.Send(new UpdateLibraryDomainCommand(id, r.Name, r.Slug, r.Description, r.HeroImageDesktop, r.HeroImageMobile, r.HeroImagePortrait, r.SortOrder, r.IsActive), ct);
        return NoContent();
    }

    /// <summary>Deactivates a library domain (soft delete).</summary>
    [HttpDelete("domains/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDomain(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLibraryDomainCommand(id), ct);
        return NoContent();
    }

    // ── Categories ────────────────────────────────────────────────────────────

    /// <summary>Returns all library categories.</summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<AdminLibraryCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAllLibraryCategoriesQuery(), ct));

    /// <summary>Creates a new library category.</summary>
    [HttpPost("categories")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateLibraryCategoryRequest r, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateLibraryCategoryCommand(r.DomainId, r.Name, r.Slug, r.Description, r.CardImage, r.SortOrder), ct);
        return Created($"/api/v1/admin/library/categories/{id}", id);
    }

    /// <summary>Updates an existing library category.</summary>
    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateLibraryCategoryRequest r, CancellationToken ct)
    {
        await _mediator.Send(new UpdateLibraryCategoryCommand(id, r.Name, r.Slug, r.Description, r.CardImage, r.SortOrder, r.IsActive), ct);
        return NoContent();
    }

    /// <summary>Deactivates a library category (soft delete).</summary>
    [HttpDelete("categories/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLibraryCategoryCommand(id), ct);
        return NoContent();
    }

    // ── Videos ─────────────────────────────────────────────────────────────────

    /// <summary>Returns all library videos (all statuses, including deleted).</summary>
    [HttpGet("videos")]
    [ProducesResponseType(typeof(List<AdminLibraryVideoListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVideos(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAllLibraryVideosQuery(), ct));

    /// <summary>Returns full edit details for a single video.</summary>
    [HttpGet("videos/{id:guid}")]
    [ProducesResponseType(typeof(AdminLibraryVideoDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVideoDetail(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetLibraryVideoEditDetailsQuery(id), ct));

    /// <summary>Creates a new library video in Draft status.</summary>
    [HttpPost("videos")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVideo([FromBody] CreateLibraryVideoRequest r, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateLibraryVideoCommand(
            r.CategoryId, r.ExpertId, r.PriceTierId, r.Title, r.Slug,
            r.Synopsis, r.Description, r.CardImage, r.HeroImage, r.IconEmoji, r.GradientClass,
            r.TrailerUrl, r.StreamUrl, r.VideoType, r.TotalDuration, r.TotalDurationSeconds,
            r.ReleaseYear, r.IsFeatured, r.FeaturedLabel, r.FeaturedPosition, r.SortOrder,
            r.Tags, r.Features?.Select(f => new CreateVideoFeatureInput(f.Icon, f.Description)).ToList()), ct);
        return Created($"/api/v1/admin/library/videos/{id}", id);
    }

    /// <summary>Updates an existing library video.</summary>
    [HttpPut("videos/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVideo(Guid id, [FromBody] UpdateLibraryVideoRequest r, CancellationToken ct)
    {
        await _mediator.Send(new UpdateLibraryVideoCommand(
            id, r.CategoryId, r.PriceTierId, r.Title, r.Slug,
            r.Synopsis, r.Description, r.CardImage, r.HeroImage, r.IconEmoji, r.GradientClass,
            r.TrailerUrl, r.StreamUrl, r.TotalDuration, r.TotalDurationSeconds,
            r.ReleaseYear, r.IsFeatured, r.FeaturedLabel, r.FeaturedPosition, r.SortOrder,
            r.Tags, r.Features?.Select(f => new CreateVideoFeatureInput(f.Icon, f.Description)).ToList()), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a library video.</summary>
    [HttpDelete("videos/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVideo(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLibraryVideoCommand(id), ct);
        return NoContent();
    }

    // ── Video Lifecycle ────────────────────────────────────────────────────────

    /// <summary>Submits a draft video for review (Draft → PendingReview).</summary>
    [HttpPost("videos/{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new SubmitLibraryVideoForReviewCommand(id), ct);
        return NoContent();
    }

    /// <summary>Publishes a video (PendingReview → Published).</summary>
    [HttpPost("videos/{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PublishVideo(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new PublishLibraryVideoCommand(id), ct);
        return NoContent();
    }

    /// <summary>Rejects a video back to draft (PendingReview → Draft).</summary>
    [HttpPost("videos/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RejectVideo(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new RejectLibraryVideoCommand(id), ct);
        return NoContent();
    }

    /// <summary>Archives a published video (Published → Archived).</summary>
    [HttpPost("videos/{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ArchiveVideo(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ArchiveLibraryVideoCommand(id), ct);
        return NoContent();
    }

    // ── Episodes ──────────────────────────────────────────────────────────────

    /// <summary>Adds an episode to a video.</summary>
    [HttpPost("videos/{videoId:guid}/episodes")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddEpisode(Guid videoId, [FromBody] AddLibraryEpisodeRequest r, CancellationToken ct)
    {
        var id = await _mediator.Send(new AddLibraryEpisodeCommand(
            videoId, r.EpisodeNumber, r.Title, r.Description, r.Duration,
            r.DurationSeconds, r.StreamUrl, r.ThumbnailUrl, r.IsFreePreview, r.SortOrder), ct);
        return Created($"/api/v1/admin/library/videos/{videoId}/episodes/{id}", id);
    }

    /// <summary>Updates an episode.</summary>
    [HttpPut("episodes/{episodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEpisode(Guid episodeId, [FromBody] UpdateLibraryEpisodeRequest r, CancellationToken ct)
    {
        await _mediator.Send(new UpdateLibraryEpisodeCommand(
            episodeId, r.EpisodeNumber, r.Title, r.Description, r.Duration,
            r.DurationSeconds, r.StreamUrl, r.ThumbnailUrl, r.IsFreePreview, r.SortOrder), ct);
        return NoContent();
    }

    /// <summary>Deletes an episode.</summary>
    [HttpDelete("episodes/{episodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEpisode(Guid episodeId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLibraryEpisodeCommand(episodeId), ct);
        return NoContent();
    }

    // ── Video Price Overrides ─────────────────────────────────────────────────

    /// <summary>Adds a per-video price override for a location.</summary>
    [HttpPost("videos/{videoId:guid}/prices")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddVideoPrice(Guid videoId, [FromBody] AddLibraryVideoPriceRequest r, CancellationToken ct)
    {
        var id = await _mediator.Send(new AddLibraryVideoPriceCommand(
            videoId, r.LocationCode, r.Amount, r.CurrencyCode, r.CurrencySymbol, r.OriginalAmount), ct);
        return Created($"/api/v1/admin/library/video-prices/{id}", id);
    }

    /// <summary>Updates a video price override.</summary>
    [HttpPut("video-prices/{priceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVideoPrice(Guid priceId, [FromBody] UpdateLibraryVideoPriceRequest r, CancellationToken ct)
    {
        await _mediator.Send(new UpdateLibraryVideoPriceCommand(priceId, r.Amount, r.CurrencyCode, r.CurrencySymbol, r.OriginalAmount), ct);
        return NoContent();
    }

    /// <summary>Deletes a video price override.</summary>
    [HttpDelete("video-prices/{priceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVideoPrice(Guid priceId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLibraryVideoPriceCommand(priceId), ct);
        return NoContent();
    }

    // ── Testimonials ──────────────────────────────────────────────────────────

    /// <summary>Adds a testimonial to a video.</summary>
    [HttpPost("videos/{videoId:guid}/testimonials")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddTestimonial(Guid videoId, [FromBody] AddLibraryTestimonialRequest r, CancellationToken ct)
    {
        var id = await _mediator.Send(new AddLibraryTestimonialCommand(
            videoId, r.ReviewerName, r.ReviewText, r.Rating, r.SortOrder), ct);
        return Created($"/api/v1/admin/library/testimonials/{id}", id);
    }

    /// <summary>Updates a testimonial.</summary>
    [HttpPut("testimonials/{testimonialId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTestimonial(Guid testimonialId, [FromBody] UpdateLibraryTestimonialRequest r, CancellationToken ct)
    {
        await _mediator.Send(new UpdateLibraryTestimonialCommand(
            testimonialId, r.ReviewerName, r.ReviewText, r.Rating, r.SortOrder, r.IsActive), ct);
        return NoContent();
    }

    /// <summary>Deletes a testimonial.</summary>
    [HttpDelete("testimonials/{testimonialId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTestimonial(Guid testimonialId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLibraryTestimonialCommand(testimonialId), ct);
        return NoContent();
    }

    // ── Filter Types ──────────────────────────────────────────────────────────

    /// <summary>Returns all filter types.</summary>
    [HttpGet("filter-types")]
    [ProducesResponseType(typeof(List<AdminFilterTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilterTypes(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAllLibraryFilterTypesQuery(), ct));

    /// <summary>Creates a filter type.</summary>
    [HttpPost("filter-types")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFilterType([FromBody] CreateLibraryFilterTypeRequest r, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateLibraryFilterTypeCommand(
            r.DomainId, r.Name, r.FilterKey, r.FilterTarget, r.SortOrder), ct);
        return Created($"/api/v1/admin/library/filter-types/{id}", id);
    }

    /// <summary>Updates a filter type.</summary>
    [HttpPut("filter-types/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFilterType(Guid id, [FromBody] UpdateLibraryFilterTypeRequest r, CancellationToken ct)
    {
        await _mediator.Send(new UpdateLibraryFilterTypeCommand(id, r.Name, r.FilterKey, r.FilterTarget, r.SortOrder, r.IsActive), ct);
        return NoContent();
    }

    /// <summary>Deletes a filter type.</summary>
    [HttpDelete("filter-types/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFilterType(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLibraryFilterTypeCommand(id), ct);
        return NoContent();
    }

    // ── Price Tiers ───────────────────────────────────────────────────────────

    /// <summary>Returns all price tiers with their regional prices.</summary>
    [HttpGet("price-tiers")]
    [ProducesResponseType(typeof(List<AdminPriceTierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceTiers(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAllLibraryPriceTiersQuery(), ct));

    /// <summary>Adds a regional price to a tier.</summary>
    [HttpPost("price-tiers/{tierId:guid}/prices")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddTierPrice(Guid tierId, [FromBody] AddLibraryTierPriceRequest r, CancellationToken ct)
    {
        var id = await _mediator.Send(new AddLibraryTierPriceCommand(
            tierId, r.LocationCode, r.Amount, r.CurrencyCode, r.CurrencySymbol), ct);
        return Created($"/api/v1/admin/library/tier-prices/{id}", id);
    }

    /// <summary>Updates a tier price.</summary>
    [HttpPut("tier-prices/{priceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTierPrice(Guid priceId, [FromBody] UpdateLibraryTierPriceRequest r, CancellationToken ct)
    {
        await _mediator.Send(new UpdateLibraryTierPriceCommand(priceId, r.Amount, r.CurrencyCode, r.CurrencySymbol), ct);
        return NoContent();
    }

    /// <summary>Deletes a tier price.</summary>
    [HttpDelete("tier-prices/{priceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTierPrice(Guid priceId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteLibraryTierPriceCommand(priceId), ct);
        return NoContent();
    }
}

// ── Request body records ─────────────────────────────────────────────────────

/// <summary>Request body for POST /api/v1/admin/library/domains.</summary>
public record CreateLibraryDomainRequest(string Name, string Slug, string? Description, string? HeroImageDesktop, string? HeroImageMobile, string? HeroImagePortrait, int SortOrder = 0);

/// <summary>Request body for PUT /api/v1/admin/library/domains/{id}.</summary>
public record UpdateLibraryDomainRequest(string? Name, string? Slug, string? Description, string? HeroImageDesktop, string? HeroImageMobile, string? HeroImagePortrait, int? SortOrder, bool? IsActive);

/// <summary>Request body for POST /api/v1/admin/library/categories.</summary>
public record CreateLibraryCategoryRequest(Guid DomainId, string Name, string Slug, string? Description, string? CardImage, int SortOrder = 0);

/// <summary>Request body for PUT /api/v1/admin/library/categories/{id}.</summary>
public record UpdateLibraryCategoryRequest(string? Name, string? Slug, string? Description, string? CardImage, int? SortOrder, bool? IsActive);

/// <summary>Feature input for video create/update.</summary>
public record LibraryFeatureInput(string Icon, string Description);

/// <summary>Request body for POST /api/v1/admin/library/videos.</summary>
public record CreateLibraryVideoRequest(
    Guid CategoryId, Guid ExpertId, Guid PriceTierId,
    string Title, string Slug, string? Synopsis, string? Description,
    string? CardImage, string? HeroImage, string? IconEmoji, string? GradientClass,
    string? TrailerUrl, string? StreamUrl, string VideoType,
    string? TotalDuration, int? TotalDurationSeconds, string? ReleaseYear,
    bool IsFeatured = false, string? FeaturedLabel = null, int? FeaturedPosition = null,
    int SortOrder = 0, List<string>? Tags = null, List<LibraryFeatureInput>? Features = null);

/// <summary>Request body for PUT /api/v1/admin/library/videos/{id}.</summary>
public record UpdateLibraryVideoRequest(
    Guid? CategoryId = null, Guid? PriceTierId = null,
    string? Title = null, string? Slug = null, string? Synopsis = null, string? Description = null,
    string? CardImage = null, string? HeroImage = null, string? IconEmoji = null, string? GradientClass = null,
    string? TrailerUrl = null, string? StreamUrl = null,
    string? TotalDuration = null, int? TotalDurationSeconds = null, string? ReleaseYear = null,
    bool? IsFeatured = null, string? FeaturedLabel = null, int? FeaturedPosition = null,
    int? SortOrder = null, List<string>? Tags = null, List<LibraryFeatureInput>? Features = null);

/// <summary>Request body for POST /api/v1/admin/library/videos/{videoId}/episodes.</summary>
public record AddLibraryEpisodeRequest(
    int EpisodeNumber, string Title, string? Description,
    string? Duration, int? DurationSeconds, string? StreamUrl,
    string? ThumbnailUrl, bool IsFreePreview = false, int SortOrder = 0);

/// <summary>Request body for PUT /api/v1/admin/library/episodes/{episodeId}.</summary>
public record UpdateLibraryEpisodeRequest(
    int? EpisodeNumber = null, string? Title = null, string? Description = null,
    string? Duration = null, int? DurationSeconds = null, string? StreamUrl = null,
    string? ThumbnailUrl = null, bool? IsFreePreview = null, int? SortOrder = null);

/// <summary>Request body for POST /api/v1/admin/library/videos/{videoId}/prices.</summary>
public record AddLibraryVideoPriceRequest(
    string LocationCode, decimal Amount, string CurrencyCode,
    string CurrencySymbol, decimal? OriginalAmount = null);

/// <summary>Request body for PUT /api/v1/admin/library/video-prices/{priceId}.</summary>
public record UpdateLibraryVideoPriceRequest(
    decimal? Amount = null, string? CurrencyCode = null,
    string? CurrencySymbol = null, decimal? OriginalAmount = null);

/// <summary>Request body for POST /api/v1/admin/library/videos/{videoId}/testimonials.</summary>
public record AddLibraryTestimonialRequest(
    string ReviewerName, string ReviewText, int Rating, int SortOrder = 0);

/// <summary>Request body for PUT /api/v1/admin/library/testimonials/{testimonialId}.</summary>
public record UpdateLibraryTestimonialRequest(
    string? ReviewerName = null, string? ReviewText = null,
    int? Rating = null, int? SortOrder = null, bool? IsActive = null);

/// <summary>Request body for POST /api/v1/admin/library/filter-types.</summary>
public record CreateLibraryFilterTypeRequest(
    Guid DomainId, string Name, string FilterKey, string FilterTarget, int SortOrder = 0);

/// <summary>Request body for PUT /api/v1/admin/library/filter-types/{id}.</summary>
public record UpdateLibraryFilterTypeRequest(
    string? Name = null, string? FilterKey = null,
    string? FilterTarget = null, int? SortOrder = null, bool? IsActive = null);

/// <summary>Request body for POST /api/v1/admin/library/price-tiers/{tierId}/prices.</summary>
public record AddLibraryTierPriceRequest(
    string LocationCode, decimal Amount, string CurrencyCode, string CurrencySymbol);

/// <summary>Request body for PUT /api/v1/admin/library/tier-prices/{priceId}.</summary>
public record UpdateLibraryTierPriceRequest(
    decimal? Amount = null, string? CurrencyCode = null, string? CurrencySymbol = null);
