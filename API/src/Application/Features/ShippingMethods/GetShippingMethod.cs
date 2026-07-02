using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.ShippingMethods;

public record GetShippingMethodQuery(Guid Id) : IRequest<ShippingMethodDto>;

public class GetShippingMethodQueryHandler : IRequestHandler<GetShippingMethodQuery, ShippingMethodDto>
{
    private readonly IApplicationDbContext _db;

    public GetShippingMethodQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ShippingMethodDto> Handle(GetShippingMethodQuery request, CancellationToken cancellationToken)
    {
        var method = await _db.ShippingMethods
            .AsNoTracking()
            .Include(m => m.ZoneRates)
                .ThenInclude(r => r.ShippingZone)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ShippingMethod), request.Id);

        return ShippingMethodDto.From(method);
    }
}
