using MediatR;

namespace FemVed.Application.Library.Commands.UpdateLibraryTierPrice;

/// <summary>Updates a regional price within a library price tier.</summary>
public record UpdateLibraryTierPriceCommand(
    Guid PriceId, decimal? Amount, string? CurrencyCode, string? CurrencySymbol) : IRequest;
