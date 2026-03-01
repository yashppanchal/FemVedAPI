using MediatR;

namespace FemVed.Application.Guided.Commands.DeleteDurationPrice;

/// <summary>
/// Deactivates a duration price row (sets <c>IsActive = false</c>).
/// Data is preserved in the database; the price simply stops appearing in the public catalog.
/// Experts may only deactivate prices on their own DRAFT or PENDING_REVIEW programs.
/// Admins may deactivate any price at any program status.
/// </summary>
/// <param name="PriceId">The price row to deactivate.</param>
/// <param name="DurationId">The duration that owns this price (used for verification).</param>
/// <param name="ProgramId">The program that owns this duration (used for ownership verification).</param>
/// <param name="RequestingUserId">Authenticated user ID.</param>
/// <param name="IsAdmin">True when the caller has the Admin role.</param>
public record DeleteDurationPriceCommand(
    Guid PriceId,
    Guid DurationId,
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest;
