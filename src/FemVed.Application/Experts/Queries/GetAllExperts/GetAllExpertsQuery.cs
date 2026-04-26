using FemVed.Application.Experts.DTOs;
using MediatR;

namespace FemVed.Application.Experts.Queries.GetAllExperts;

/// <summary>
/// Returns every expert profile in the platform — used by the anonymous
/// <c>GET /api/v1/experts</c> endpoint.
/// </summary>
public sealed record GetAllExpertsQuery : IRequest<List<PublicExpertDto>>;
