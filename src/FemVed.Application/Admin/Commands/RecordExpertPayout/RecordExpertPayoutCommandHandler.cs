using System.Text.Json;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.RecordExpertPayout;

/// <summary>
/// Handles <see cref="RecordExpertPayoutCommand"/>.
/// Creates an expert_payouts row and writes an audit log entry.
/// Guards against overpayment: the payout amount must not exceed the expert's
/// outstanding balance (earned share minus previously paid) in the requested currency.
/// </summary>
public sealed class RecordExpertPayoutCommandHandler
    : IRequestHandler<RecordExpertPayoutCommand, ExpertPayoutRecordDto>
{
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["GBP"] = "£",  ["USD"] = "$",   ["INR"] = "₹",  ["AUD"] = "A$",
        ["EUR"] = "€",  ["AED"] = "د.إ", ["NZD"] = "NZ$",["SGD"] = "S$",
        ["MYR"] = "RM", ["ZAR"] = "R",   ["LKR"] = "₨"
    };

    private readonly IRepository<ExpertPayout> _payouts;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Order> _orders;
    private readonly IRepository<User> _admins;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RecordExpertPayoutCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public RecordExpertPayoutCommandHandler(
        IRepository<ExpertPayout> payouts,
        IRepository<Expert> experts,
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<Order> orders,
        IRepository<User> admins,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<RecordExpertPayoutCommandHandler> logger)
    {
        _payouts   = payouts;
        _experts   = experts;
        _programs  = programs;
        _durations = durations;
        _orders    = orders;
        _admins    = admins;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>
    /// Records the payout and logs the action.
    /// Throws <see cref="DomainException"/> if the amount exceeds the outstanding balance.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when the expert profile does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the payout amount exceeds the outstanding balance.</exception>
    public async Task<ExpertPayoutRecordDto> Handle(
        RecordExpertPayoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("RecordExpertPayout: admin {AdminId} recording payout for expert {ExpertId} — {Amount} {Currency}",
            request.AdminUserId, request.ExpertId, request.Amount, request.CurrencyCode);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.Id == request.ExpertId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Expert), request.ExpertId);

        // ── Outstanding balance guard ───────────────────────────────────────────
        // Compute what the expert has earned in this currency and subtract what
        // has already been paid out, to get the outstanding balance.
        var programs  = await _programs.GetAllAsync(p => p.ExpertId == expert.Id && !p.IsDeleted, cancellationToken);
        var programIds = programs.Select(p => p.Id).ToHashSet();

        var durations = await _durations.GetAllAsync(d => programIds.Contains(d.ProgramId), cancellationToken);
        var durationIds = durations.Select(d => d.Id).ToHashSet();

        var paidOrders = await _orders.GetAllAsync(
            o => o.Status == OrderStatus.Paid
              && durationIds.Contains(o.DurationId.GetValueOrDefault())
              && o.CurrencyCode == request.CurrencyCode.ToUpperInvariant(),
            cancellationToken);

        var totalEarned = paidOrders.Sum(o => o.AmountPaid);
        var expertShareEarned = Math.Round(totalEarned * (expert.CommissionRate / 100m), 2);

        var existingPayouts = await _payouts.GetAllAsync(
            p => p.ExpertId == expert.Id && p.CurrencyCode == request.CurrencyCode.ToUpperInvariant(),
            cancellationToken);

        var totalAlreadyPaid = existingPayouts.Sum(p => p.Amount);
        var outstandingBalance = Math.Round(expertShareEarned - totalAlreadyPaid, 2);

        if (request.Amount > outstandingBalance)
            throw new DomainException(
                $"Payout of {request.Amount} {request.CurrencyCode.ToUpperInvariant()} exceeds the outstanding balance of {outstandingBalance} {request.CurrencyCode.ToUpperInvariant()}. " +
                $"Expert share earned: {expertShareEarned}, already paid: {totalAlreadyPaid}.");

        var payout = new ExpertPayout
        {
            Id               = Guid.NewGuid(),
            ExpertId         = request.ExpertId,
            Amount           = request.Amount,
            CurrencyCode     = request.CurrencyCode.ToUpperInvariant(),
            PaymentReference = request.PaymentReference?.Trim(),
            Notes            = request.Notes?.Trim(),
            PaidBy           = request.AdminUserId,
            PaidAt           = request.PaidAt,
            CreatedAt        = DateTimeOffset.UtcNow
        };

        await _payouts.AddAsync(payout);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "RECORD_EXPERT_PAYOUT",
            EntityType  = "expert_payouts",
            EntityId    = payout.Id,
            BeforeValue = null,
            AfterValue  = JsonSerializer.Serialize(new
            {
                payout.ExpertId,
                payout.Amount,
                payout.CurrencyCode,
                payout.PaymentReference,
                payout.PaidAt
            }),
            IpAddress = request.IpAddress,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("RecordExpertPayout: payout {PayoutId} recorded for expert {ExpertId}",
            payout.Id, expert.Id);

        // Load admin name for the response
        var adminUser = await _admins.FirstOrDefaultAsync(u => u.Id == request.AdminUserId, cancellationToken);
        var adminName = adminUser is not null
            ? $"{adminUser.FirstName} {adminUser.LastName}".Trim()
            : "Admin";

        return new ExpertPayoutRecordDto(
            PayoutId:        payout.Id,
            ExpertId:        expert.Id,
            ExpertName:      expert.DisplayName,
            Amount:          payout.Amount,
            CurrencyCode:    payout.CurrencyCode,
            CurrencySymbol:  CurrencySymbols.GetValueOrDefault(payout.CurrencyCode, payout.CurrencyCode),
            PaymentReference:payout.PaymentReference,
            Notes:           payout.Notes,
            PaidBy:          payout.PaidBy,
            PaidByName:      adminName,
            PaidAt:          payout.PaidAt,
            CreatedAt:       payout.CreatedAt);
    }
}
