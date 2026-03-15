using FemVed.Application.Experts.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAllEnrollments;

/// <summary>
/// Handles <see cref="GetAllEnrollmentsQuery"/>.
/// Loads all UserProgramAccess records across all experts, batch-fetches related
/// users, programs, durations, and experts, and maps to <see cref="EnrollmentDto"/>.
/// Supports optional filtering by access status and expert ID.
/// </summary>
public sealed class GetAllEnrollmentsQueryHandler : IRequestHandler<GetAllEnrollmentsQuery, List<EnrollmentDto>>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<User> _users;
    private readonly IRepository<Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Expert> _experts;
    private readonly ILogger<GetAllEnrollmentsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetAllEnrollmentsQueryHandler(
        IRepository<UserProgramAccess> access,
        IRepository<User> users,
        IRepository<Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<Expert> experts,
        ILogger<GetAllEnrollmentsQueryHandler> logger)
    {
        _access    = access;
        _users     = users;
        _programs  = programs;
        _durations = durations;
        _experts   = experts;
        _logger    = logger;
    }

    /// <summary>Returns enrollments ordered by enrollment date descending, with optional status and expert filters.</summary>
    /// <param name="request">The query, optionally containing status and expert ID filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Flat list of enrollment DTOs.</returns>
    public async Task<List<EnrollmentDto>> Handle(GetAllEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetAllEnrollments: loading enrollments (statusFilter={StatusFilter}, expertId={ExpertId})",
            request.StatusFilter, request.ExpertId);

        // Parse optional status filter
        UserProgramAccessStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
            Enum.TryParse<UserProgramAccessStatus>(request.StatusFilter, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        // Load access records with optional filters
        List<UserProgramAccess> accessRecords;
        if (statusFilter.HasValue && request.ExpertId.HasValue)
            accessRecords = (await _access.GetAllAsync(a => a.Status == statusFilter.Value && a.ExpertId == request.ExpertId.Value, cancellationToken)).ToList();
        else if (statusFilter.HasValue)
            accessRecords = (await _access.GetAllAsync(a => a.Status == statusFilter.Value, cancellationToken)).ToList();
        else if (request.ExpertId.HasValue)
            accessRecords = (await _access.GetAllAsync(a => a.ExpertId == request.ExpertId.Value, cancellationToken)).ToList();
        else
            accessRecords = (await _access.GetAllAsync(cancellationToken: cancellationToken)).ToList();

        if (accessRecords.Count == 0)
        {
            _logger.LogInformation("GetAllEnrollments: no enrollments found");
            return new List<EnrollmentDto>();
        }

        // Batch-load related entities
        var userIds     = accessRecords.Select(a => a.UserId).Distinct().ToHashSet();
        var programIds  = accessRecords.Select(a => a.ProgramId).Distinct().ToHashSet();
        var durationIds = accessRecords.Select(a => a.DurationId).Distinct().ToHashSet();
        var expertIds   = accessRecords.Select(a => a.ExpertId).Distinct().ToHashSet();

        var users     = await _users.GetAllAsync(u => userIds.Contains(u.Id), cancellationToken);
        var programs  = await _programs.GetAllAsync(p => programIds.Contains(p.Id), cancellationToken);
        var durations = await _durations.GetAllAsync(d => durationIds.Contains(d.Id), cancellationToken);
        var experts   = await _experts.GetAllAsync(e => expertIds.Contains(e.Id), cancellationToken);

        var userMap     = users.ToDictionary(u => u.Id);
        var programMap  = programs.ToDictionary(p => p.Id);
        var durationMap = durations.ToDictionary(d => d.Id);
        var expertMap   = experts.ToDictionary(e => e.Id);

        var result = accessRecords
            .OrderByDescending(a => a.CreatedAt)
            .Select(a =>
            {
                userMap.TryGetValue(a.UserId, out var user);
                programMap.TryGetValue(a.ProgramId, out var prog);
                durationMap.TryGetValue(a.DurationId, out var dur);
                expertMap.TryGetValue(a.ExpertId, out var expert);

                return new EnrollmentDto(
                    AccessId:        a.Id,
                    OrderId:         a.OrderId,
                    UserId:          a.UserId,
                    UserFirstName:   user?.FirstName   ?? string.Empty,
                    UserLastName:    user?.LastName    ?? string.Empty,
                    UserEmail:       user?.Email       ?? string.Empty,
                    ProgramId:       a.ProgramId,
                    ProgramName:     prog?.Name        ?? "Unknown Program",
                    DurationLabel:   dur?.Label        ?? "Unknown Duration",
                    DurationWeeks:   dur?.Weeks        ?? 0,
                    AccessStatus:    a.Status.ToString(),
                    StartedAt:       a.StartedAt,
                    PausedAt:        a.PausedAt,
                    CompletedAt:     a.CompletedAt,
                    EndedBy:         a.EndedBy,
                    EndedByRole:     a.EndedByRole,
                    EnrolledAt:          a.CreatedAt,
                    ExpertId:            a.ExpertId,
                    ExpertName:          expert?.DisplayName,
                    ScheduledStartAt:    a.ScheduledStartAt,
                    EndDate:             a.EndDate,
                    RequestedStartDate:  a.RequestedStartDate,
                    StartRequestStatus:  a.StartRequestStatus?.ToString());
            })
            .ToList();

        _logger.LogInformation("GetAllEnrollments: returned {Count} enrollments", result.Count);
        return result;
    }
}
