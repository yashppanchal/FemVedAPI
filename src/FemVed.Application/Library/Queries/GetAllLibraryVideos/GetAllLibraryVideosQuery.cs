using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetAllLibraryVideos;

/// <summary>Returns all library videos (all statuses) for admin management.</summary>
public record GetAllLibraryVideosQuery : IRequest<List<AdminLibraryVideoListItem>>;
