using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetExpertPayoutHistory;

/// <summary>
/// Handles <see cref="GetExpertPayoutHistoryQuery"/>.
/// Returns every recorded payout for a specific expert, newest first.
/// </summary>
public sealed class GetExpertPayoutHistoryQueryHandler
    : IRequestHandler<GetExpertPayoutHistoryQuery, List<ExpertPayoutRecordDto>>
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
    private readonly ILogger<GetExpertPayoutHistoryQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetExpertPayoutHistoryQueryHandler(
        IRepository<ExpertPayout> payouts,
        IRepository<Expert> experts,
        IRepository<User> users,
        ILogger<GetExpertPayoutHistoryQueryHandler> logger)
    {
        _payouts = payouts;
        _experts = experts;
        _users   = users;
        _logger  = logger;
    }

    /// <summary>Returns all payout records for the given expert, newest first.</summary>
    /// <exception cref="NotFoundException">Thrown when the expert profile does not exist.</exception>
    public async Task<List<ExpertPayoutRecordDto>> Handle(
        GetExpertPayoutHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetExpertPayoutHistory: loading payouts for expert {ExpertId}", request.ExpertId);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.Id == request.ExpertId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Expert), request.ExpertId);

        var payouts = await _payouts.GetAllAsync(
            p => p.ExpertId == request.ExpertId, cancellationToken);

        if (payouts.Count == 0)
        {
            _logger.LogInformation("GetExpertPayoutHistory: no payouts for expert {ExpertId}", request.ExpertId);
            return new List<ExpertPayoutRecordDto>();
        }

        // Batch-load admins who made the payments
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
                    : "Unknown Admin";

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

        _logger.LogInformation("GetExpertPayoutHistory: returned {Count} payouts for expert {ExpertId}",
            result.Count, request.ExpertId);

        return result;
    }
}
