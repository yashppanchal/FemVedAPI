using FemVed.Application.Experts.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Queries.GetMyEnrollments;

/// <summary>
/// Handles <see cref="GetMyEnrollmentsQuery"/>.
/// Loads all UserProgramAccess records for the expert, then batch-fetches
/// users, programs, and durations to build the enrollment DTOs.
/// </summary>
public sealed class GetMyEnrollmentsQueryHandler : IRequestHandler<GetMyEnrollmentsQuery, List<EnrollmentDto>>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<User> _users;
    private readonly IRepository<Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly ILogger<GetMyEnrollmentsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetMyEnrollmentsQueryHandler(
        IRepository<UserProgramAccess> access,
        IRepository<User> users,
        IRepository<Program> programs,
        IRepository<ProgramDuration> durations,
        ILogger<GetMyEnrollmentsQueryHandler> logger)
    {
        _access   = access;
        _users    = users;
        _programs = programs;
        _durations = durations;
        _logger   = logger;
    }

    /// <summary>Returns all enrollments for the authenticated expert, newest first.</summary>
    /// <param name="request">The query containing the expert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of enrollment DTOs ordered by enrollment date descending.</returns>
    public async Task<List<EnrollmentDto>> Handle(GetMyEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyEnrollments: loading enrollments for expert {ExpertId}", request.ExpertId);

        var accessRecords = await _access.GetAllAsync(
            a => a.ExpertId == request.ExpertId,
            cancellationToken);

        if (!accessRecords.Any())
        {
            _logger.LogInformation("GetMyEnrollments: no enrollments for expert {ExpertId}", request.ExpertId);
            return new List<EnrollmentDto>();
        }

        // Batch-load related entities
        var userIds     = accessRecords.Select(a => a.UserId).Distinct().ToHashSet();
        var programIds  = accessRecords.Select(a => a.ProgramId).Distinct().ToHashSet();
        var durationIds = accessRecords.Select(a => a.DurationId).Distinct().ToHashSet();

        var users     = await _users.GetAllAsync(u => userIds.Contains(u.Id), cancellationToken);
        var programs  = await _programs.GetAllAsync(p => programIds.Contains(p.Id), cancellationToken);
        var durations = await _durations.GetAllAsync(d => durationIds.Contains(d.Id), cancellationToken);

        var userMap     = users.ToDictionary(u => u.Id);
        var programMap  = programs.ToDictionary(p => p.Id);
        var durationMap = durations.ToDictionary(d => d.Id);

        var result = accessRecords
            .OrderByDescending(a => a.CreatedAt)
            .Select(a =>
            {
                userMap.TryGetValue(a.UserId, out var user);
                programMap.TryGetValue(a.ProgramId, out var prog);
                durationMap.TryGetValue(a.DurationId, out var dur);

                return new EnrollmentDto(
                    AccessId:      a.Id,
                    OrderId:       a.OrderId,
                    UserId:        a.UserId,
                    UserFirstName: user?.FirstName  ?? string.Empty,
                    UserLastName:  user?.LastName   ?? string.Empty,
                    UserEmail:     user?.Email      ?? string.Empty,
                    ProgramId:     a.ProgramId,
                    ProgramName:   prog?.Name       ?? "Unknown Program",
                    DurationLabel: dur?.Label       ?? "Unknown Duration",
                    AccessStatus:  a.Status.ToString(),
                    StartedAt:     a.StartedAt,
                    CompletedAt:   a.CompletedAt,
                    EnrolledAt:    a.CreatedAt);
            })
            .ToList();

        _logger.LogInformation("GetMyEnrollments: returned {Count} enrollments for expert {ExpertId}",
            result.Count, request.ExpertId);

        return result;
    }
}
