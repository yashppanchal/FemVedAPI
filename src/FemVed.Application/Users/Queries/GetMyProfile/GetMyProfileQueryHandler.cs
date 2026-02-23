using FemVed.Application.Interfaces;
using FemVed.Application.Users.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Users.Queries.GetMyProfile;

/// <summary>Handles <see cref="GetMyProfileQuery"/>. Loads the user record and maps it to <see cref="UserProfileDto"/>.</summary>
public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    private readonly IRepository<User> _users;
    private readonly ILogger<GetMyProfileQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetMyProfileQueryHandler(IRepository<User> users, ILogger<GetMyProfileQueryHandler> logger)
    {
        _users  = users;
        _logger = logger;
    }

    /// <summary>Fetches and returns the authenticated user's profile.</summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's profile DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the user record does not exist.</exception>
    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyProfile: loading profile for user {UserId}", request.UserId);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        _logger.LogInformation("GetMyProfile: profile loaded for user {UserId}", request.UserId);

        return new UserProfileDto(
            UserId:          user.Id,
            Email:           user.Email,
            FirstName:       user.FirstName,
            LastName:        user.LastName,
            CountryCode:     user.CountryDialCode,
            CountryIsoCode:  user.CountryIsoCode,
            MobileNumber:    user.MobileNumber,
            FullMobile:      user.FullMobile,
            WhatsAppOptIn:   user.WhatsAppOptIn,
            IsEmailVerified: user.IsEmailVerified,
            CreatedAt:       user.CreatedAt);
    }
}
