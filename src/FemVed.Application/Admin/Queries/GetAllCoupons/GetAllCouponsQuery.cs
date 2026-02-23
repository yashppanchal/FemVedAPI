using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAllCoupons;

/// <summary>Returns all coupons (active and inactive) ordered by creation date descending.</summary>
public record GetAllCouponsQuery : IRequest<List<CouponDto>>;
