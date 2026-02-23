using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAllUsers;

/// <summary>Handles <see cref="GetAllUsersQuery"/>. Returns all users with role name, ordered newest first.</summary>
public sealed class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<AdminUserDto>>
{
    private readonly IRepository<User> _users;
    private readonly ILogger<GetAllUsersQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllUsersQueryHandler(IRepository<User> users, ILogger<GetAllUsersQueryHandler> logger)
    {
        _users  = users;
        _logger = logger;
    }

    /// <summary>Returns all user accounts ordered by creation date descending.</summary>
    public async Task<List<AdminUserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllUsers: loading all users");

        var users = await _users.GetAllAsync(cancellationToken: cancellationToken);

        var result = users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto(
                UserId:          u.Id,
                Email:           u.Email,
                FirstName:       u.FirstName,
                LastName:        u.LastName,
                RoleId:          u.RoleId,
                RoleName:        u.Role?.Name ?? u.RoleId switch { 1 => "Admin", 2 => "Expert", _ => "User" },
                CountryIsoCode:  u.CountryIsoCode,
                FullMobile:      u.FullMobile,
                IsEmailVerified: u.IsEmailVerified,
                WhatsAppOptIn:   u.WhatsAppOptIn,
                IsActive:        u.IsActive,
                IsDeleted:       u.IsDeleted,
                CreatedAt:       u.CreatedAt))
            .ToList();

        _logger.LogInformation("GetAllUsers: returned {Count} users", result.Count);
        return result;
    }
}
