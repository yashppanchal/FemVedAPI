using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetLibraryPurchases;

/// <summary>
/// Returns all library purchase records (who bought what).
/// Admin only.
/// </summary>
public record GetLibraryPurchasesQuery : IRequest<LibraryPurchasesResponse>;
