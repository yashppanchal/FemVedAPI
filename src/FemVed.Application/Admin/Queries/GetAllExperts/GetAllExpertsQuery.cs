using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAllExperts;

/// <summary>Returns all expert profiles (including inactive) ordered by creation date descending.</summary>
public record GetAllExpertsQuery : IRequest<List<AdminExpertDto>>;
