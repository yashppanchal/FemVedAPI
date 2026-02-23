using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAllUsers;

/// <summary>Returns all user accounts (including soft-deleted) ordered by creation date descending.</summary>
public record GetAllUsersQuery : IRequest<List<AdminUserDto>>;
