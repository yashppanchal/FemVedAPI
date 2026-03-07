using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Queries.GetMyPayoutHistory;

/// <summary>
/// Handles <see cref="GetMyPayoutHistoryQuery"/>.
/// Returns every recorded payout received by the authenticated expert, newest first.
/// </summary>
public sealed class GetMyPayoutHistoryQueryHandler
    : IRequestHandler<GetMyPayoutHistoryQuery, List<ExpertPayoutRecordDto>>
{
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["GBP"] = "£",  ["USD"] = "$",   ["INR"] = "₹",  ["AUD"] = "A$",
        ["EUR"] = "€",  ["AED"] = "د.إ", ["NZD"] = "NZ$",["SGD"] = "S$",
        ["MYR"] = "RM", ["ZAR"] = "R",   ["LKR"] = "₨"
    };

    private readonly IRepository<ExpertPayout> _payouts;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<User> _users;
    private readonly ILogger<GetMyPayoutHistoryQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetMyPayoutHistoryQueryHandler(
        IRepository<ExpertPayout> payouts,
        IRepository<Expert> experts,
        IRepository<User> users,
        ILogger<GetMyPayoutHistoryQueryHandler> logger)
    {
        _payouts = payouts;
        _experts = experts;
        _users   = users;
        _logger  = logger;
    }

    /// <summary>Returns all payout records for the authenticated expert, newest first.</summary>
    /// <exception cref="NotFoundException">Thrown when no expert profile is linked to the user.</exception>
    public async Task<List<ExpertPayoutRecordDto>> Handle(
        GetMyPayoutHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyPayoutHistory: loading payouts for user {UserId}", request.UserId);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.UserId == request.UserId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Expert profile", request.UserId);

        var payouts = await _payouts.GetAllAsync(
            p => p.ExpertId == expert.Id, cancellationToken);

        if (payouts.Count == 0)
        {
            _logger.LogInformation("GetMyPayoutHistory: no payouts yet for expert {ExpertId}", expert.Id);
            return new List<ExpertPayoutRecordDto>();
        }

        // Batch-load admins who made the payments (for display name only)
        var adminIds = payouts.Select(p => p.PaidBy).Distinct().ToHashSet();
        var admins   = await _users.GetAllAsync(u => adminIds.Contains(u.Id), cancellationToken);
        var adminMap = admins.ToDictionary(u => u.Id);

        var result = payouts
            .OrderByDescending(p => p.PaidAt)
            .Select(p =>
            {
                adminMap.TryGetValue(p.PaidBy, out var admin);
                var adminName = admin is not null
                    ? $"{admin.FirstName} {admin.LastName}".Trim()
                    : "Admin";

                return new ExpertPayoutRecordDto(
                    PayoutId:        p.Id,
                    ExpertId:        expert.Id,
                    ExpertName:      expert.DisplayName,
                    Amount:          p.Amount,
                    CurrencyCode:    p.CurrencyCode,
                    CurrencySymbol:  CurrencySymbols.GetValueOrDefault(p.CurrencyCode, p.CurrencyCode),
                    PaymentReference:p.PaymentReference,
                    Notes:           p.Notes,
                    PaidBy:          p.PaidBy,
                    PaidByName:      adminName,
                    PaidAt:          p.PaidAt,
                    CreatedAt:       p.CreatedAt);
            })
            .ToList();

        _logger.LogInformation("GetMyPayoutHistory: returned {Count} payouts for expert {ExpertId}",
            result.Count, expert.Id);

        return result;
    }
}
