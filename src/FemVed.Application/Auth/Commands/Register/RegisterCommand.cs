using FemVed.Application.Auth.DTOs;
using MediatR;

namespace FemVed.Application.Auth.Commands.Register;

/// <summary>
/// Registers a new user account with role User (role_id = 3).
/// Sends a verification email after successful registration.
/// </summary>
/// <param name="Email">Unique email address used for login.</param>
/// <param name="Password">Plain-text password (min 8 chars, must contain upper, lower, digit, special char). Hashed with BCrypt work factor 12.</param>
/// <param name="FirstName">User's first name.</param>
/// <param name="LastName">User's last name.</param>
/// <param name="CountryCode">Optional dial code e.g. "+91". Derives ISO code for payment gateway routing.</param>
/// <param name="MobileNumber">Optional mobile digits only (no dial code). Required when CountryCode is provided.</param>
public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? CountryCode,
    string? MobileNumber) : IRequest<AuthResponse>;
