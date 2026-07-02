using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Manufacturers;

public record GetManufacturerQuery(Guid Id) : IRequest<ManufacturerDto>;

public class GetManufacturerQueryHandler : IRequestHandler<GetManufacturerQuery, ManufacturerDto>
{
    private readonly IApplicationDbContext _db;

    public GetManufacturerQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ManufacturerDto> Handle(GetManufacturerQuery request, CancellationToken cancellationToken)
    {
        var manufacturer = await _db.Manufacturers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Manufacturer), request.Id);

        return ManufacturerDto.From(manufacturer);
    }
}
