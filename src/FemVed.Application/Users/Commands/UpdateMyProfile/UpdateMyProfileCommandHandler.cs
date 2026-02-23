using FemVed.Application.Interfaces;
using FemVed.Application.Users.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using FemVed.Domain.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Users.Commands.UpdateMyProfile;

/// <summary>
/// Handles <see cref="UpdateMyProfileCommand"/>.
/// Updates editable profile fields and re-derives ISO code and FullMobile from the new dial code.
/// </summary>
public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, UserProfileDto>
{
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateMyProfileCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateMyProfileCommandHandler(
        IRepository<User> users,
        IUnitOfWork uow,
        ILogger<UpdateMyProfileCommandHandler> logger)
    {
        _users  = users;
        _uow    = uow;
        _logger = logger;
    }

    /// <summary>Applies the profile update and returns the refreshed profile.</summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the user record does not exist.</exception>
    public async Task<UserProfileDto> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateMyProfile: updating profile for user {UserId}", request.UserId);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var isoCode = DialCodeMapper.ToIsoCode(request.CountryCode);
        var fullMobile = string.IsNullOrEmpty(request.CountryCode) || string.IsNullOrEmpty(request.MobileNumber)
            ? null
            : $"{request.CountryCode}{request.MobileNumber}";

        user.FirstName       = request.FirstName.Trim();
        user.LastName        = request.LastName.Trim();
        user.CountryDialCode = request.CountryCode?.Trim();
        user.CountryIsoCode  = isoCode;
        user.MobileNumber    = request.MobileNumber?.Trim();
        user.FullMobile      = fullMobile;
        user.WhatsAppOptIn   = request.WhatsAppOptIn;
        user.UpdatedAt       = DateTimeOffset.UtcNow;

        _users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateMyProfile: profile updated successfully for user {UserId}", request.UserId);

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
