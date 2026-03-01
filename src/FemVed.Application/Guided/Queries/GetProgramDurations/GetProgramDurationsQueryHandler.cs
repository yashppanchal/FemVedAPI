using FemVed.Application.Guided.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Queries.GetProgramDurations;

/// <summary>
/// Handles <see cref="GetProgramDurationsQuery"/>.
/// Returns all durations (and all their prices for every location) for a program.
/// Experts may only query their own programs. Admins may query any program.
/// </summary>
public sealed class GetProgramDurationsQueryHandler
    : IRequestHandler<GetProgramDurationsQuery, List<DurationManagementDto>>
{
    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly ILogger<GetProgramDurationsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetProgramDurationsQueryHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        ILogger<GetProgramDurationsQueryHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _durations = durations;
        _prices    = prices;
        _logger    = logger;
    }

    /// <summary>Returns durations with all location prices for the specified program.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of durations, each with all their prices.</returns>
    /// <exception cref="NotFoundException">Thrown when the program or (filtered) duration is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when a non-admin caller does not own the program.</exception>
    public async Task<List<DurationManagementDto>> Handle(
        GetProgramDurationsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetProgramDurations: program {ProgramId}, user {UserId}, durationFilter {DurationId}",
            request.ProgramId, request.RequestingUserId, request.DurationId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Program), request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("You do not have an expert profile.");

            if (expert.Id != program.ExpertId)
                throw new ForbiddenException("You can only view durations for your own programs.");
        }

        // Load durations — optionally filtered to a single one
        var durationRows = await _durations.GetAllAsync(
            d => d.ProgramId == request.ProgramId
              && (request.DurationId == null || d.Id == request.DurationId),
            cancellationToken);

        if (request.DurationId.HasValue && durationRows.Count == 0)
            throw new NotFoundException(nameof(ProgramDuration), request.DurationId.Value);

        var result = new List<DurationManagementDto>();

        foreach (var dur in durationRows)
        {
            var priceRows = await _prices.GetAllAsync(
                p => p.DurationId == dur.Id, cancellationToken);

            result.Add(new DurationManagementDto(
                DurationId: dur.Id,
                Label:      dur.Label,
                Weeks:      dur.Weeks,
                SortOrder:  dur.SortOrder,
                IsActive:   dur.IsActive,
                Prices: priceRows.Select(p => new DurationPriceManagementDto(
                    PriceId:        p.Id,
                    LocationCode:   p.LocationCode,
                    Amount:         p.Amount,
                    CurrencyCode:   p.CurrencyCode,
                    CurrencySymbol: p.CurrencySymbol,
                    IsActive:       p.IsActive)).ToList()));
        }

        _logger.LogInformation(
            "GetProgramDurations: returned {Count} duration(s) for program {ProgramId}",
            result.Count, request.ProgramId);

        return result;
    }
}
