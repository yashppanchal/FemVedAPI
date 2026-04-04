using MediatR;

namespace FemVed.Application.Library.Commands.DeleteLibraryTierPrice;

/// <summary>Removes a regional price from a library price tier.</summary>
public record DeleteLibraryTierPriceCommand(Guid PriceId) : IRequest;
