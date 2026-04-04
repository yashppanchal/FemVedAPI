using MediatR;

namespace FemVed.Application.Library.Commands.UpdateLibraryVideoPrice;

/// <summary>Updates a per-video price override.</summary>
public record UpdateLibraryVideoPriceCommand(
    Guid PriceId, decimal? Amount, string? CurrencyCode,
    string? CurrencySymbol, decimal? OriginalAmount) : IRequest;
