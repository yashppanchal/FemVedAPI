using MediatR;

namespace FemVed.Application.Library.Commands.AddLibraryTierPrice;

/// <summary>Adds a regional price to a library price tier.</summary>
public record AddLibraryTierPriceCommand(
    Guid TierId, string LocationCode, decimal Amount,
    string CurrencyCode, string CurrencySymbol) : IRequest<Guid>;
