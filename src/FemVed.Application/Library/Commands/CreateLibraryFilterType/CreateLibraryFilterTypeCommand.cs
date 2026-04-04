using MediatR;

namespace FemVed.Application.Library.Commands.CreateLibraryFilterType;

/// <summary>Creates a new filter type for the library catalog.</summary>
public record CreateLibraryFilterTypeCommand(
    Guid DomainId, string Name, string FilterKey,
    string FilterTarget, int SortOrder) : IRequest<Guid>;
