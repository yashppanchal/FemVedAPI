using MediatR;

namespace FemVed.Application.Guided.Commands.AddDurationPrice;

/// <summary>
/// Adds a new location-specific price to an existing duration.
/// Only one active price per <paramref name="LocationCode"/> is allowed on a duration —
/// the handler rejects the request if an active price for that location already exists.
/// Experts may only add prices to durations on their own DRAFT or PENDING_REVIEW programs.
/// Admins may add prices to any program at any status.
/// </summary>
/// <param name="DurationId">The duration to add the price to.</param>
/// <param name="ProgramId">The program that owns this duration (used for ownership verification).</param>
/// <param name="RequestingUserId">Authenticated user ID.</param>
/// <param name="IsAdmin">True when the caller has the Admin role.</param>
/// <param name="LocationCode">ISO country code, e.g. "AU", "IN", "GB".</param>
/// <param name="Amount">Price amount, e.g. 450.00.</param>
/// <param name="CurrencyCode">ISO 4217 code, e.g. "AUD".</param>
/// <param name="CurrencySymbol">Display symbol, e.g. "A$".</param>
public record AddDurationPriceCommand(
    Guid DurationId,
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    string LocationCode,
    decimal Amount,
    string CurrencyCode,
    string CurrencySymbol) : IRequest<Guid>;
