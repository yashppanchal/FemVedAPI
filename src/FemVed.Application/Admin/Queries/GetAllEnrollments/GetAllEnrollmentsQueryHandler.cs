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
/// users, programs, and durations, and maps to <see cref="EnrollmentDto"/>.
/// </summary>
public sealed class GetAllEnrollmentsQueryHandler : IRequestHandler<GetAllEnrollmentsQuery, List<EnrollmentDto>>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<User> _users;
    private readonly IRepository<Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly ILogger<GetAllEnrollmentsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetAllEnrollmentsQueryHandler(
        IRepository<UserProgramAccess> access,
        IRepository<User> users,
        IRepository<Program> programs,
        IRepository<ProgramDuration> durations,
        ILogger<GetAllEnrollmentsQueryHandler> logger)
    {
        _access    = access;
        _users     = users;
        _programs  = programs;
        _durations = durations;
        _logger    = logger;
    }

    /// <summary>Returns all enrollments ordered by enrollment date descending, with optional status filter.</summary>
    /// <param name="request">The query, optionally containing an access-status filter string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Flat list of enrollment DTOs.</returns>
    public async Task<List<EnrollmentDto>> Handle(GetAllEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllEnrollments: loading all enrollments (statusFilter={StatusFilter})", request.StatusFilter);

        // Parse optional status filter
        UserProgramAccessStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
            Enum.TryParse<UserProgramAccessStatus>(request.StatusFilter, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        // Load all access records — admin sees everything
        var accessRecords = statusFilter.HasValue
            ? await _access.GetAllAsync(a => a.Status == statusFilter.Value, cancellationToken)
            : await _access.GetAllAsync(cancellationToken: cancellationToken);

        if (accessRecords.Count == 0)
        {
            _logger.LogInformation("GetAllEnrollments: no enrollments found");
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
                    PausedAt:      a.PausedAt,
                    CompletedAt:   a.CompletedAt,
                    EndedBy:       a.EndedBy,
                    EndedByRole:   a.EndedByRole,
                    EnrolledAt:    a.CreatedAt);
            })
            .ToList();

        _logger.LogInformation("GetAllEnrollments: returned {Count} enrollments", result.Count);
        return result;
    }
}
