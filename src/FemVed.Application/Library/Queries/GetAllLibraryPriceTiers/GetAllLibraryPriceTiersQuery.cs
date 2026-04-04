using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetAllLibraryPriceTiers;

/// <summary>Returns all library price tiers with their regional prices for admin management.</summary>
public record GetAllLibraryPriceTiersQuery : IRequest<List<AdminPriceTierDto>>;
