using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateLibraryTestimonial;

/// <summary>Handles <see cref="UpdateLibraryTestimonialCommand"/>.</summary>
public sealed class UpdateLibraryTestimonialCommandHandler : IRequestHandler<UpdateLibraryTestimonialCommand>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryVideoTestimonial> _testimonials;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateLibraryTestimonialCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public UpdateLibraryTestimonialCommandHandler(IRepository<LibraryVideoTestimonial> testimonials, IUnitOfWork uow, IMemoryCache cache, ILogger<UpdateLibraryTestimonialCommandHandler> logger)
    { _testimonials = testimonials; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Updates the testimonial.</summary>
    public async Task Handle(UpdateLibraryTestimonialCommand request, CancellationToken cancellationToken)
    {
        var t = await _testimonials.FirstOrDefaultAsync(x => x.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideoTestimonial), request.TestimonialId);
        if (request.ReviewerName is not null) t.ReviewerName = request.ReviewerName.Trim();
        if (request.ReviewText is not null) t.ReviewText = request.ReviewText.Trim();
        if (request.Rating.HasValue) t.Rating = request.Rating.Value;
        if (request.SortOrder.HasValue) t.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) t.IsActive = request.IsActive.Value;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        _testimonials.Update(t);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
    }
}
