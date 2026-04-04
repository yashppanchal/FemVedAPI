using MediatR;

namespace FemVed.Application.Library.Commands.DeleteLibraryVideoPrice;

/// <summary>Removes a per-video price override.</summary>
public record DeleteLibraryVideoPriceCommand(Guid PriceId) : IRequest;
