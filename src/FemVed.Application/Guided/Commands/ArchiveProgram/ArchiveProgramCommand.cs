using MediatR;

namespace FemVed.Application.Guided.Commands.ArchiveProgram;

/// <summary>
/// Transitions a program from PUBLISHED to ARCHIVED. AdminOnly.
/// Evicts the guided tree cache so the program disappears immediately.
/// </summary>
/// <param name="ProgramId">The program to archive.</param>
public record ArchiveProgramCommand(Guid ProgramId) : IRequest;
