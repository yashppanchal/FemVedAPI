using FemVed.Application.Experts.DTOs;
using MediatR;

namespace FemVed.Application.Experts.Queries.GetMyEnrollments;

/// <summary>
/// Returns all enrollment records across all programs belonging to the authenticated expert,
/// newest first. Includes enrolled user details so the expert can identify who to contact.
/// </summary>
/// <param name="ExpertId">The authenticated expert's ID.</param>
public record GetMyEnrollmentsQuery(Guid ExpertId) : IRequest<List<EnrollmentDto>>;
