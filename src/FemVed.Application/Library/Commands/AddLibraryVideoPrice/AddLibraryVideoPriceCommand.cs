using MediatR;

namespace FemVed.Application.Library.Commands.AddLibraryVideoPrice;

/// <summary>Adds a per-video price override for a specific location.</summary>
public record AddLibraryVideoPriceCommand(
    Guid VideoId, string LocationCode, decimal Amount,
    string CurrencyCode, string CurrencySymbol, decimal? OriginalAmount) : IRequest<Guid>;
