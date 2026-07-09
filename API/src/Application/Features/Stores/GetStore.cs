using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Stores;

public record GetStoreQuery(Guid Id) : IRequest<StoreDto>;

public class GetStoreQueryHandler : IRequestHandler<GetStoreQuery, StoreDto>
{
    private readonly IApplicationDbContext _db;

    public GetStoreQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<StoreDto> Handle(GetStoreQuery request, CancellationToken cancellationToken)
    {
        var store = await _db.Stores
            .Include(s => s.Address)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Store), request.Id);

        return StoreDto.From(store);
    }
}
