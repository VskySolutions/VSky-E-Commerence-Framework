using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Coupons;

/// <summary>Returns a page of coupon codes ordered by code, optionally filtered by code, discount or active state.</summary>
public record ListCouponsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? DiscountId = null,
    bool? IsActive = null) : IRequest<PaginatedList<CouponDto>>;

public class ListCouponsQueryHandler : IRequestHandler<ListCouponsQuery, PaginatedList<CouponDto>>
{
    private readonly IApplicationDbContext _db;

    public ListCouponsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<CouponDto>> Handle(ListCouponsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<CouponCode> query = _db.CouponCodes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => c.Code.Contains(term));
        }

        if (request.DiscountId.HasValue)
            query = query.Where(c => c.DiscountId == request.DiscountId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        var ordered = query.OrderBy(c => c.Code);

        var page = await PaginatedList<CouponCode>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(CouponDto.From).ToList();
        return new PaginatedList<CouponDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
