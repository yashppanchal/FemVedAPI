using FemVed.Application.Interfaces;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.RejectProgram;

/// <summary>
/// Handles <see cref="RejectProgramCommand"/>.
/// Transitions PENDING_REVIEW → DRAFT so the expert can revise and resubmit.
/// </summary>
public sealed class RejectProgramCommandHandler : IRequestHandler<RejectProgramCommand>
{
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RejectProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public RejectProgramCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IUnitOfWork uow,
        ILogger<RejectProgramCommandHandler> logger)
    {
        _programs = programs;
        _uow      = uow;
        _logger   = logger;
    }

    /// <summary>Rejects the program back to draft status.</summary>
    /// <param name="request">The reject command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the program is not found.</exception>
    /// <exception cref="DomainException">Thrown when the program is not in PENDING_REVIEW status.</exception>
    public async Task Handle(RejectProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rejecting program {ProgramId} back to Draft", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (program.Status != ProgramStatus.PendingReview)
            throw new DomainException($"Only PENDING_REVIEW programs can be rejected. Current status: {program.Status}.");

        program.Status    = ProgramStatus.Draft;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Program {ProgramId} rejected back to Draft.", request.ProgramId);
    }
}
