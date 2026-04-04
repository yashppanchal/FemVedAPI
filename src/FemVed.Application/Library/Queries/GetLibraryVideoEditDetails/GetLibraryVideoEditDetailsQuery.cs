using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetLibraryVideoEditDetails;

/// <summary>Returns full edit details for a single library video (admin use).</summary>
/// <param name="VideoId">The video to load.</param>
public record GetLibraryVideoEditDetailsQuery(Guid VideoId) : IRequest<AdminLibraryVideoDetail>;
