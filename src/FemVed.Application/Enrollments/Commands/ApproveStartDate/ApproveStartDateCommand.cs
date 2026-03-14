using MediatR;

namespace FemVed.Application.Enrollments.Commands.ApproveStartDate;

/// <summary>
/// Expert or admin approves the user's requested start date.
/// Sets <c>StartRequestStatus = Approved</c> and schedules the enrollment
/// by copying <c>RequestedStartDate</c> to <c>ScheduledStartAt</c>.
/// </summary>
/// <param name="AccessId">UUID of the <c>UserProgramAccess</c> record.</param>
/// <param name="PerformedByUserId">Authenticated user's ID (expert or admin).</param>
/// <param name="IsAdmin">True when the caller holds the Admin role.</param>
public record ApproveStartDateCommand(
    Guid AccessId,
    Guid PerformedByUserId,
    bool IsAdmin) : IRequest;
