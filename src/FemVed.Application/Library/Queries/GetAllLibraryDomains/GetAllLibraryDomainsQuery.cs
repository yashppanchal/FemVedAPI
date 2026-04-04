using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetAllLibraryDomains;

/// <summary>Returns all library domains for admin management.</summary>
public record GetAllLibraryDomainsQuery : IRequest<List<AdminLibraryDomainDto>>;
