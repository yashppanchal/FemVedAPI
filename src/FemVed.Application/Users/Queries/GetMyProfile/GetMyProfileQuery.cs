using FemVed.Application.Users.DTOs;
using MediatR;

namespace FemVed.Application.Users.Queries.GetMyProfile;

/// <summary>Returns the full profile of the currently authenticated user.</summary>
/// <param name="UserId">The authenticated user's ID (from JWT).</param>
public record GetMyProfileQuery(Guid UserId) : IRequest<UserProfileDto>;
