using MediatR;

namespace FemVed.Application.Enrollments.Commands.DeclineStartDate;

/// <summary>
/// Expert or admin declines the user's requested start date.
/// Sets <c>StartRequestStatus = Declined</c> and clears <c>RequestedStartDate</c>.
/// </summary>
/// <param name="AccessId">UUID of the <c>UserProgramAccess</c> record.</param>
/// <param name="PerformedByUserId">Authenticated user's ID (expert or admin).</param>
/// <param name="IsAdmin">True when the caller holds the Admin role.</param>
public record DeclineStartDateCommand(
    Guid AccessId,
    Guid PerformedByUserId,
    bool IsAdmin) : IRequest;
