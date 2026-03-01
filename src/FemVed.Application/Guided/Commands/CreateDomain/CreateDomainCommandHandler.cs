using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.CreateDomain;

/// <summary>
/// Handles <see cref="CreateDomainCommand"/>.
/// Creates a new guided domain, returns its ID, and writes an admin audit log entry.
/// </summary>
public sealed class CreateDomainCommandHandler : IRequestHandler<CreateDomainCommand, Guid>
{
    private readonly IRepository<GuidedDomain> _domains;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateDomainCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public CreateDomainCommandHandler(
        IRepository<GuidedDomain> domains,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<CreateDomainCommandHandler> logger)
    {
        _domains   = domains;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Creates a new guided domain and logs the action to <c>admin_audit_log</c>.</summary>
    /// <param name="request">The create domain command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new domain's primary key.</returns>
    /// <exception cref="ValidationException">Thrown when the slug is already in use.</exception>
    public async Task<Guid> Handle(CreateDomainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateDomain: admin {AdminId} creating domain with slug {Slug}",
            request.AdminUserId, request.Slug);

        var slugExists = await _domains.AnyAsync(d => d.Slug == request.Slug, cancellationToken);
        if (slugExists)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "slug", [$"A domain with slug '{request.Slug}' already exists."] }
            });

        var domain = new GuidedDomain
        {
            Id        = Guid.NewGuid(),
            Name      = request.Name.Trim(),
            Slug      = request.Slug.Trim().ToLowerInvariant(),
            SortOrder = request.SortOrder,
            IsActive  = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _domains.AddAsync(domain);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "CREATE_DOMAIN",
            EntityType  = "guided_domains",
            EntityId    = domain.Id,
            BeforeValue = null,
            AfterValue  = JsonSerializer.Serialize(new { domain.Name, domain.Slug, domain.SortOrder }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CreateDomain: domain {DomainId} created with slug {Slug}", domain.Id, domain.Slug);
        return domain.Id;
    }
}
