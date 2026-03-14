using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAdminExpertPrograms;

/// <summary>
/// Handles <see cref="GetAdminExpertProgramsQuery"/>.
/// Loads all non-deleted programs for the expert, then aggregates enrollment counts from
/// UserProgramAccess records, and maps to <see cref="ExpertProgramSummaryDto"/>.
/// </summary>
public sealed class GetAdminExpertProgramsQueryHandler
    : IRequestHandler<GetAdminExpertProgramsQuery, List<ExpertProgramSummaryDto>>
{
    private readonly IRepository<Program> _programs;
    private readonly IRepository<UserProgramAccess> _access;
    private readonly ILogger<GetAdminExpertProgramsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetAdminExpertProgramsQueryHandler(
        IRepository<Program> programs,
        IRepository<UserProgramAccess> access,
        ILogger<GetAdminExpertProgramsQueryHandler> logger)
    {
        _programs = programs;
        _access   = access;
        _logger   = logger;
    }

    /// <summary>Returns program summaries with enrollment counts for the given expert.</summary>
    /// <param name="request">The query containing the expert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of program summary DTOs ordered by creation date descending.</returns>
    public async Task<List<ExpertProgramSummaryDto>> Handle(
        GetAdminExpertProgramsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetAdminExpertPrograms: loading programs for expert {ExpertId}", request.ExpertId);

        var programs = await _programs.GetAllAsync(
            p => p.ExpertId == request.ExpertId && !p.IsDeleted,
            cancellationToken);

        if (programs.Count == 0)
        {
            _logger.LogInformation("GetAdminExpertPrograms: no programs found for expert {ExpertId}", request.ExpertId);
            return new List<ExpertProgramSummaryDto>();
        }

        var programIds   = programs.Select(p => p.Id).ToHashSet();
        var accessRecords = await _access.GetAllAsync(
            a => programIds.Contains(a.ProgramId),
            cancellationToken);

        // Group enrollment counts per program
        var totalMap  = accessRecords.GroupBy(a => a.ProgramId).ToDictionary(g => g.Key, g => g.Count());
        var activeMap = accessRecords
            .Where(a => a.Status == UserProgramAccessStatus.Active)
            .GroupBy(a => a.ProgramId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = programs
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ExpertProgramSummaryDto(
                ProgramId:         p.Id,
                ProgramName:       p.Name,
                Status:            p.Status.ToString(),
                TotalEnrollments:  totalMap.TryGetValue(p.Id, out var t) ? t : 0,
                ActiveEnrollments: activeMap.TryGetValue(p.Id, out var a) ? a : 0,
                CreatedAt:         p.CreatedAt))
            .ToList();

        _logger.LogInformation(
            "GetAdminExpertPrograms: returned {Count} programs for expert {ExpertId}",
            result.Count, request.ExpertId);

        return result;
    }
}
