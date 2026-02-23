using MediatR;

namespace FemVed.Application.Guided.Commands.PublishProgram;

/// <summary>
/// Transitions a program from PENDING_REVIEW to PUBLISHED. AdminOnly.
/// Evicts the guided tree cache so the new program appears immediately.
/// </summary>
/// <param name="ProgramId">The program to publish.</param>
public record PublishProgramCommand(Guid ProgramId) : IRequest;
