using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Coupons;

public record GetCouponQuery(Guid Id) : IRequest<CouponDto>;

public class GetCouponQueryHandler : IRequestHandler<GetCouponQuery, CouponDto>
{
    private readonly IApplicationDbContext _db;

    public GetCouponQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CouponDto> Handle(GetCouponQuery request, CancellationToken cancellationToken)
    {
        var coupon = await _db.CouponCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CouponCode), request.Id);

        return CouponDto.From(coupon);
    }
}
