using MediatR;

namespace FemVed.Application.Guided.Queries.GetPublicExperts;

/// <summary>
/// Returns all active, non-deleted experts with basic display info for the public homepage.
/// Cached for 10 minutes.
/// </summary>
public record GetPublicExpertsQuery : IRequest<List<PublicExpertDto>>;
