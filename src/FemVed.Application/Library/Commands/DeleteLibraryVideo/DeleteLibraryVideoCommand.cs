using MediatR;

namespace FemVed.Application.Library.Commands.DeleteLibraryVideo;

/// <summary>
/// Soft-deletes a library video by setting IsDeleted = true.
/// </summary>
/// <param name="VideoId">The video to soft-delete.</param>
public record DeleteLibraryVideoCommand(Guid VideoId) : IRequest;
