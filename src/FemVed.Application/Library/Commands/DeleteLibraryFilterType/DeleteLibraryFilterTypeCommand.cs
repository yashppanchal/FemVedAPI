using MediatR;

namespace FemVed.Application.Library.Commands.DeleteLibraryFilterType;

/// <summary>Removes a library filter type.</summary>
public record DeleteLibraryFilterTypeCommand(Guid FilterTypeId) : IRequest;
