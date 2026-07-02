using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Discounts;

public record GetDiscountQuery(Guid Id) : IRequest<DiscountDto>;

public class GetDiscountQueryHandler : IRequestHandler<GetDiscountQuery, DiscountDto>
{
    private readonly IApplicationDbContext _db;

    public GetDiscountQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DiscountDto> Handle(GetDiscountQuery request, CancellationToken cancellationToken)
    {
        var discount = await _db.Discounts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Discount), request.Id);

        return DiscountDto.From(discount);
    }
}
