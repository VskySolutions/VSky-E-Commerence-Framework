using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.CmsPageGroups;

public record GetCmsPageGroupQuery(Guid Id) : IRequest<CmsPageGroupDto>;

public class GetCmsPageGroupQueryHandler : IRequestHandler<GetCmsPageGroupQuery, CmsPageGroupDto>
{
    private readonly IApplicationDbContext _db;

    public GetCmsPageGroupQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsPageGroupDto> Handle(GetCmsPageGroupQuery request, CancellationToken cancellationToken)
    {
        var group = await _db.CMSPageGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CMSPageGroup), request.Id);

        return CmsPageGroupDto.From(group);
    }
}
