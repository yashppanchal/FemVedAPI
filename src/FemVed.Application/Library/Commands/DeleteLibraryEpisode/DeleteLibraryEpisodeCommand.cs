using MediatR;

namespace FemVed.Application.Library.Commands.DeleteLibraryEpisode;

/// <summary>
/// Hard-deletes a library video episode by its primary key.
/// </summary>
/// <param name="EpisodeId">The episode's primary key.</param>
public record DeleteLibraryEpisodeCommand(Guid EpisodeId) : IRequest;
