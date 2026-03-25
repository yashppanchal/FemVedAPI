using MediatR;

namespace FemVed.Application.Guided.Commands.RejectProgram;

/// <summary>
/// Transitions a program from PENDING_REVIEW back to DRAFT. Admin only.
/// Used when an admin declines a program submitted for review.
/// </summary>
/// <param name="ProgramId">The program to reject.</param>
public record RejectProgramCommand(Guid ProgramId) : IRequest;
