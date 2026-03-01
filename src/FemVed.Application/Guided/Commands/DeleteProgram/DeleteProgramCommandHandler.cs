using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.DeleteProgram;

/// <summary>
/// Handles <see cref="DeleteProgramCommand"/>.
/// Sets IsDeleted = true, IsActive = false on the program, and writes an audit log entry.
/// Non-admin callers must own the program (Expert.UserId == request.UserId).
/// </summary>
public sealed class DeleteProgramCommandHandler : IRequestHandler<DeleteProgramCommand>
{
    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeleteProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteProgramCommandHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<DeleteProgramCommandHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Soft-deletes the program and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the program does not exist or is already deleted.</exception>
    /// <exception cref="ForbiddenException">Thrown when a non-admin caller does not own the program.</exception>
    public async Task Handle(DeleteProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteProgram: user {UserId} (isAdmin={IsAdmin}) soft-deleting program {ProgramId}",
            request.UserId, request.IsAdmin, request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Program), request.ProgramId);

        // Non-admin: verify the caller's Expert record owns this program
        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.UserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("You do not have an expert profile.");

            if (expert.Id != program.ExpertId)
                throw new ForbiddenException("You can only delete your own programs.");
        }

        var before = JsonSerializer.Serialize(new { program.IsDeleted, program.IsActive, program.Status });

        program.IsDeleted = true;
        program.IsActive  = false;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.UserId,
            Action      = "DELETE_PROGRAM",
            EntityType  = "programs",
            EntityId    = program.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsDeleted = true, IsActive = false }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DeleteProgram: program {ProgramId} soft-deleted", program.Id);
    }
}
