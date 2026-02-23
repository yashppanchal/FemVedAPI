using FemVed.Application.Experts.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Queries.GetMyExpertPrograms;

/// <summary>
/// Handles <see cref="GetMyExpertProgramsQuery"/>.
/// Returns all programs owned by the expert with enrollment counts.
/// </summary>
public sealed class GetMyExpertProgramsQueryHandler : IRequestHandler<GetMyExpertProgramsQuery, List<ExpertProgramSummaryDto>>
{
    private readonly IRepository<Program> _programs;
    private readonly IRepository<UserProgramAccess> _access;
    private readonly ILogger<GetMyExpertProgramsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetMyExpertProgramsQueryHandler(
        IRepository<Program> programs,
        IRepository<UserProgramAccess> access,
        ILogger<GetMyExpertProgramsQueryHandler> logger)
    {
        _programs = programs;
        _access   = access;
        _logger   = logger;
    }

    /// <summary>Returns all programs for the authenticated expert with enrollment counts.</summary>
    /// <param name="request">The query containing the expert ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of program summaries ordered by creation date descending.</returns>
    public async Task<List<ExpertProgramSummaryDto>> Handle(
        GetMyExpertProgramsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyExpertPrograms: loading programs for expert {ExpertId}", request.ExpertId);

        var programs = await _programs.GetAllAsync(
            p => p.ExpertId == request.ExpertId && !p.IsDeleted,
            cancellationToken);

        if (!programs.Any())
        {
            _logger.LogInformation("GetMyExpertPrograms: no programs for expert {ExpertId}", request.ExpertId);
            return new List<ExpertProgramSummaryDto>();
        }

        // Batch-load all access records for this expert's programs
        var programIds = programs.Select(p => p.Id).ToHashSet();
        var allAccess  = await _access.GetAllAsync(
            a => programIds.Contains(a.ProgramId),
            cancellationToken);

        // Group by program for O(1) count lookups
        var accessByProgram = allAccess.GroupBy(a => a.ProgramId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = programs
            .OrderByDescending(p => p.CreatedAt)
            .Select(p =>
            {
                accessByProgram.TryGetValue(p.Id, out var accessList);
                var total  = accessList?.Count ?? 0;
                var active = accessList?.Count(a => a.Status == UserProgramAccessStatus.Active) ?? 0;

                return new ExpertProgramSummaryDto(
                    ProgramId:         p.Id,
                    Name:              p.Name,
                    Slug:              p.Slug,
                    Status:            p.Status.ToString(),
                    GridImageUrl:      p.GridImageUrl,
                    ActiveEnrollments: active,
                    TotalEnrollments:  total,
                    CreatedAt:         p.CreatedAt,
                    UpdatedAt:         p.UpdatedAt);
            })
            .ToList();

        _logger.LogInformation("GetMyExpertPrograms: returned {Count} programs for expert {ExpertId}",
            result.Count, request.ExpertId);

        return result;
    }
}
