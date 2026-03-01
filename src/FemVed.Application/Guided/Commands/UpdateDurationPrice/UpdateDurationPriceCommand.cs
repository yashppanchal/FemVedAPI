using MediatR;

namespace FemVed.Application.Guided.Commands.UpdateDurationPrice;

/// <summary>
/// Updates amount and/or currency details of an existing duration price row.
/// Only non-null fields are applied (partial update).
/// Experts may only update prices on their own DRAFT or PENDING_REVIEW programs.
/// Admins may update any program at any status.
/// </summary>
/// <param name="PriceId">The price row to update.</param>
/// <param name="DurationId">The duration that owns this price (used for verification).</param>
/// <param name="ProgramId">The program that owns this duration (used for ownership verification).</param>
/// <param name="RequestingUserId">Authenticated user ID.</param>
/// <param name="IsAdmin">True when the caller has the Admin role.</param>
/// <param name="Amount">New price amount. Null to leave unchanged.</param>
/// <param name="CurrencyCode">New ISO 4217 currency code. Null to leave unchanged.</param>
/// <param name="CurrencySymbol">New display symbol. Null to leave unchanged.</param>
public record UpdateDurationPriceCommand(
    Guid PriceId,
    Guid DurationId,
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    decimal? Amount,
    string? CurrencyCode,
    string? CurrencySymbol) : IRequest;
