using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.CmsPages;

public record GetCmsPageQuery(Guid Id) : IRequest<CmsPageDto>;

public class GetCmsPageQueryHandler : IRequestHandler<GetCmsPageQuery, CmsPageDto>
{
    private readonly IApplicationDbContext _db;

    public GetCmsPageQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsPageDto> Handle(GetCmsPageQuery request, CancellationToken cancellationToken)
    {
        var page = await _db.CMSPages
            .AsNoTracking()
            .Include(p => p.PageGroup)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CMSPage), request.Id);

        return CmsPageDto.From(page);
    }
}
