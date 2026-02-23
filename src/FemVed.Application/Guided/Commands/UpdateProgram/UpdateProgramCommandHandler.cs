using FemVed.Application.Interfaces;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.UpdateProgram;

/// <summary>
/// Handles <see cref="UpdateProgramCommand"/>.
/// Applies partial updates. Experts can only edit programs they own in DRAFT or PENDING_REVIEW status.
/// </summary>
public sealed class UpdateProgramCommandHandler : IRequestHandler<UpdateProgramCommand>
{
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<Domain.Entities.Expert> _experts;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateProgramCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<Domain.Entities.Expert> experts,
        IUnitOfWork uow,
        ILogger<UpdateProgramCommandHandler> logger)
    {
        _programs = programs;
        _experts = experts;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Applies partial updates to the program.</summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the program is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when an Expert tries to edit another expert's program.</exception>
    /// <exception cref="DomainException">Thrown when a PUBLISHED or ARCHIVED program is updated by a non-Admin.</exception>
    public async Task Handle(UpdateProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating program {ProgramId}", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted,
                cancellationToken)
                ?? throw new ForbiddenException("Expert profile not found.");

            if (program.ExpertId != expert.Id)
                throw new ForbiddenException("You can only edit your own programs.");

            if (program.Status is ProgramStatus.Published or ProgramStatus.Archived)
                throw new DomainException("Published or archived programs cannot be edited.");
        }

        if (request.Name is not null) program.Name = request.Name.Trim();
        if (request.GridDescription is not null) program.GridDescription = request.GridDescription.Trim();
        if (request.GridImageUrl is not null) program.GridImageUrl = request.GridImageUrl.Trim();
        if (request.Overview is not null) program.Overview = request.Overview.Trim();
        if (request.SortOrder is not null) program.SortOrder = request.SortOrder.Value;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Program {ProgramId} updated successfully", request.ProgramId);
    }
}
