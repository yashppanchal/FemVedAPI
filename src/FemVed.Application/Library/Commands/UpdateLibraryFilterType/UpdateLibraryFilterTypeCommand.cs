using MediatR;

namespace FemVed.Application.Library.Commands.UpdateLibraryFilterType;

/// <summary>Updates a library filter type.</summary>
public record UpdateLibraryFilterTypeCommand(
    Guid FilterTypeId, string? Name, string? FilterKey,
    string? FilterTarget, int? SortOrder, bool? IsActive) : IRequest;
