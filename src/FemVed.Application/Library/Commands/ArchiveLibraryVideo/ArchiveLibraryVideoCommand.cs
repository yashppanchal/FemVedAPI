using MediatR;

namespace FemVed.Application.Library.Commands.ArchiveLibraryVideo;

/// <summary>Transitions a library video from Published to Archived status.</summary>
/// <param name="VideoId">The video to archive.</param>
public record ArchiveLibraryVideoCommand(Guid VideoId) : IRequest;
