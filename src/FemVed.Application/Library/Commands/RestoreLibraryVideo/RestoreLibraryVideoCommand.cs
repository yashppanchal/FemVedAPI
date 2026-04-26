using MediatR;

namespace FemVed.Application.Library.Commands.RestoreLibraryVideo;

/// <summary>
/// Restores a previously archived (or soft-deleted) library video so it is visible
/// in the public catalog again. Archived videos transition back to Published.
/// </summary>
/// <param name="VideoId">Id of the video to restore.</param>
public sealed record RestoreLibraryVideoCommand(Guid VideoId) : IRequest;
