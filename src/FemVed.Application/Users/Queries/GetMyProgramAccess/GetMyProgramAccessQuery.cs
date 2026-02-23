using FemVed.Application.Users.DTOs;
using MediatR;

namespace FemVed.Application.Users.Queries.GetMyProgramAccess;

/// <summary>Returns all program access records belonging to the authenticated user, newest first.</summary>
/// <param name="UserId">The authenticated user's ID (from JWT).</param>
public record GetMyProgramAccessQuery(Guid UserId) : IRequest<List<ProgramAccessDto>>;
