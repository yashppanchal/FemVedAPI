using MediatR;

namespace FemVed.Application.Guided.Commands.DeleteProgram;

/// <summary>
/// Soft-deletes a guided program (sets IsDeleted = true, IsActive = false).
/// Admins may delete any program.
/// Experts may only delete their own programs.
/// </summary>
/// <param name="ProgramId">The program to soft-delete.</param>
/// <param name="UserId">ID of the user performing the action (used for ownership check and audit log).</param>
/// <param name="IsAdmin">True when the caller has the Admin role — skips ownership check.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record DeleteProgramCommand(Guid ProgramId, Guid UserId, bool IsAdmin, string? IpAddress) : IRequest;
