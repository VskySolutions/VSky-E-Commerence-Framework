using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.ProductAttributes;

public record GetProductAttributeQuery(Guid Id) : IRequest<ProductAttributeDto>;

public class GetProductAttributeQueryHandler : IRequestHandler<GetProductAttributeQuery, ProductAttributeDto>
{
    private readonly IApplicationDbContext _db;

    public GetProductAttributeQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductAttributeDto> Handle(GetProductAttributeQuery request, CancellationToken cancellationToken)
    {
        var entity = await _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Values)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductAttribute), request.Id);

        return ProductAttributeDto.From(entity);
    }
}
