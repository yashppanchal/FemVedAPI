using MediatR;

namespace FemVed.Application.Library.Commands.UpdateWatchProgress;

/// <summary>
/// Updates watch progress for a purchased library video.
/// For Masterclass: updates overall progress on <c>user_library_access</c>.
/// For Series: updates per-episode progress on <c>user_episode_progress</c> and recalculates overall.
/// </summary>
/// <param name="UserId">Authenticated user's ID.</param>
/// <param name="VideoId">The purchased video's ID.</param>
/// <param name="ProgressSeconds">Current playback position in seconds.</param>
/// <param name="EpisodeId">Episode ID (required for Series, null for Masterclass).</param>
public record UpdateWatchProgressCommand(
    Guid UserId,
    Guid VideoId,
    int ProgressSeconds,
    Guid? EpisodeId = null) : IRequest;
