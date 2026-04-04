using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.DeleteLibraryTestimonial;

/// <summary>Handles <see cref="DeleteLibraryTestimonialCommand"/>.</summary>
public sealed class DeleteLibraryTestimonialCommandHandler : IRequestHandler<DeleteLibraryTestimonialCommand>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryVideoTestimonial> _testimonials;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteLibraryTestimonialCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeleteLibraryTestimonialCommandHandler(IRepository<LibraryVideoTestimonial> testimonials, IUnitOfWork uow, IMemoryCache cache, ILogger<DeleteLibraryTestimonialCommandHandler> logger)
    { _testimonials = testimonials; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Removes the testimonial.</summary>
    public async Task Handle(DeleteLibraryTestimonialCommand request, CancellationToken cancellationToken)
    {
        var t = await _testimonials.FirstOrDefaultAsync(x => x.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideoTestimonial), request.TestimonialId);
        _testimonials.Remove(t);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
    }
}
