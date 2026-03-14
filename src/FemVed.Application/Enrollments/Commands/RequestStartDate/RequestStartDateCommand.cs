using MediatR;

namespace FemVed.Application.Enrollments.Commands.RequestStartDate;

/// <summary>
/// Allows an enrolled user to submit a preferred start date for their program.
/// The expert or admin must then approve or decline the request.
/// </summary>
/// <param name="AccessId">UUID of the <c>UserProgramAccess</c> record.</param>
/// <param name="UserId">The authenticated user's ID (must match the enrollment's UserId).</param>
/// <param name="RequestedStartDate">The user's preferred start date.</param>
public record RequestStartDateCommand(
    Guid AccessId,
    Guid UserId,
    DateOnly RequestedStartDate) : IRequest;
