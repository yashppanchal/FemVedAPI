using FemVed.Application.Interfaces;
using FemVed.Application.Users.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Users.Queries.GetMyProgramAccess;

/// <summary>
/// Handles <see cref="GetMyProgramAccessQuery"/>.
/// Loads all program access records for the user, batch-fetches related programs,
/// experts, and durations, then projects to <see cref="ProgramAccessDto"/>.
/// </summary>
public sealed class GetMyProgramAccessQueryHandler : IRequestHandler<GetMyProgramAccessQuery, List<ProgramAccessDto>>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly ILogger<GetMyProgramAccessQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetMyProgramAccessQueryHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramDuration> durations,
        ILogger<GetMyProgramAccessQueryHandler> logger)
    {
        _access   = access;
        _programs = programs;
        _experts  = experts;
        _durations = durations;
        _logger   = logger;
    }

    /// <summary>Fetches all program access records for the authenticated user.</summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of program access DTOs ordered by purchase date descending.</returns>
    public async Task<List<ProgramAccessDto>> Handle(GetMyProgramAccessQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyProgramAccess: loading access records for user {UserId}", request.UserId);

        var accessRecords = await _access.GetAllAsync(
            a => a.UserId == request.UserId,
            cancellationToken);

        if (!accessRecords.Any())
        {
            _logger.LogInformation("GetMyProgramAccess: no access records for user {UserId}", request.UserId);
            return new List<ProgramAccessDto>();
        }

        // Batch-load related entities to avoid N+1 queries
        var programIds  = accessRecords.Select(a => a.ProgramId).Distinct().ToHashSet();
        var expertIds   = accessRecords.Select(a => a.ExpertId).Distinct().ToHashSet();
        var durationIds = accessRecords.Select(a => a.DurationId).Distinct().ToHashSet();

        var programs  = await _programs.GetAllAsync(p => programIds.Contains(p.Id), cancellationToken);
        var experts   = await _experts.GetAllAsync(e => expertIds.Contains(e.Id), cancellationToken);
        var durations = await _durations.GetAllAsync(d => durationIds.Contains(d.Id), cancellationToken);

        var programMap  = programs.ToDictionary(p => p.Id);
        var expertMap   = experts.ToDictionary(e => e.Id);
        var durationMap = durations.ToDictionary(d => d.Id);

        var result = accessRecords
            .OrderByDescending(a => a.CreatedAt)
            .Select(a =>
            {
                programMap.TryGetValue(a.ProgramId, out var prog);
                expertMap.TryGetValue(a.ExpertId, out var exp);
                durationMap.TryGetValue(a.DurationId, out var dur);

                return new ProgramAccessDto(
                    AccessId:        a.Id,
                    OrderId:         a.OrderId,
                    ProgramId:       a.ProgramId,
                    ProgramName:     prog?.Name      ?? "Unknown Program",
                    ProgramImageUrl: prog?.GridImageUrl,
                    ExpertId:        a.ExpertId,
                    ExpertName:      exp?.DisplayName ?? "Unknown Expert",
                    DurationLabel:   dur?.Label       ?? "Unknown Duration",
                    Status:          a.Status.ToString(),
                    StartedAt:       a.StartedAt,
                    CompletedAt:     a.CompletedAt,
                    PurchasedAt:     a.CreatedAt);
            })
            .ToList();

        _logger.LogInformation("GetMyProgramAccess: returned {Count} records for user {UserId}", result.Count, request.UserId);

        return result;
    }
}
