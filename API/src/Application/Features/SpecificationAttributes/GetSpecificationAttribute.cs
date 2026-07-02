using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.SpecificationAttributes;

public record GetSpecificationAttributeQuery(Guid Id) : IRequest<SpecificationAttributeDto>;

public class GetSpecificationAttributeQueryHandler : IRequestHandler<GetSpecificationAttributeQuery, SpecificationAttributeDto>
{
    private readonly IApplicationDbContext _db;

    public GetSpecificationAttributeQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SpecificationAttributeDto> Handle(GetSpecificationAttributeQuery request, CancellationToken cancellationToken)
    {
        var entity = await _db.SpecificationAttributes
            .AsNoTracking()
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SpecificationAttribute), request.Id);

        return SpecificationAttributeDto.From(entity);
    }
}
