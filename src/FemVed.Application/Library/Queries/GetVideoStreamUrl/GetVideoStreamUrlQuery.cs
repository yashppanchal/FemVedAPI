using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetVideoStreamUrl;

/// <summary>
/// Returns stream URL(s) for a purchased library video.
/// Only succeeds if the user has an active <c>user_library_access</c> record for this video.
/// For Masterclass: returns the single stream URL.
/// For Series: returns all episode stream URLs with per-episode watch progress.
/// Free preview episodes are always returned regardless of purchase status.
/// </summary>
/// <param name="Slug">Video URL slug.</param>
/// <param name="UserId">Authenticated user's ID.</param>
public record GetVideoStreamUrlQuery(string Slug, Guid UserId) : IRequest<LibraryStreamResponse>;
