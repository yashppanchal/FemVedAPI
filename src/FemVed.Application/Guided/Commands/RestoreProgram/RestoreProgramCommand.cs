using MediatR;

namespace FemVed.Application.Guided.Commands.RestoreProgram;

/// <summary>
/// Restores a previously archived or soft-deleted guided program so it is visible
/// in the public catalog again. Archived programs transition back to PUBLISHED;
/// soft-deleted programs have their <c>IsDeleted</c> flag cleared and their
/// <c>IsActive</c> flag re-enabled. AdminOnly.
/// </summary>
/// <param name="ProgramId">The program to restore.</param>
public record RestoreProgramCommand(Guid ProgramId) : IRequest;
