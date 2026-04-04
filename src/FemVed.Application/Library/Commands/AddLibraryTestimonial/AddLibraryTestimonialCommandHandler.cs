using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.AddLibraryTestimonial;

/// <summary>Handles <see cref="AddLibraryTestimonialCommand"/>.</summary>
public sealed class AddLibraryTestimonialCommandHandler : IRequestHandler<AddLibraryTestimonialCommand, Guid>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryVideoTestimonial> _testimonials;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AddLibraryTestimonialCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public AddLibraryTestimonialCommandHandler(IRepository<LibraryVideo> videos, IRepository<LibraryVideoTestimonial> testimonials, IUnitOfWork uow, IMemoryCache cache, ILogger<AddLibraryTestimonialCommandHandler> logger)
    { _videos = videos; _testimonials = testimonials; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Creates a testimonial.</summary>
    public async Task<Guid> Handle(AddLibraryTestimonialCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding testimonial for video {VideoId}", request.VideoId);
        if (!await _videos.AnyAsync(v => v.Id == request.VideoId && !v.IsDeleted, cancellationToken))
            throw new NotFoundException(nameof(LibraryVideo), request.VideoId);
        var testimonial = new LibraryVideoTestimonial
        {
            Id = Guid.NewGuid(), VideoId = request.VideoId,
            ReviewerName = request.ReviewerName.Trim(), ReviewText = request.ReviewText.Trim(),
            Rating = request.Rating, SortOrder = request.SortOrder,
            IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        await _testimonials.AddAsync(testimonial);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
        _logger.LogInformation("Testimonial {Id} created for video {VideoId}", testimonial.Id, request.VideoId);
        return testimonial.Id;
    }
}
