using FemVed.Application.Guided.DTOs;
using MediatR;

namespace FemVed.Application.Guided.Queries.GetGuidedTree;

/// <summary>
/// Returns the full guided catalog tree for the React frontend.
/// The response is cached in memory for 10 minutes per location code.
/// </summary>
/// <param name="LocationCode">
/// ISO country code used to select the correct price tier ("IN", "GB", "US", etc.).
/// Detected by the controller from the authenticated user's profile, or the
/// Accept-Language header, or defaults to "GB".
/// </param>
public record GetGuidedTreeQuery(string LocationCode) : IRequest<GuidedTreeResponse>;
