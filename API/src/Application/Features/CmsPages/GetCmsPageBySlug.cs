using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CmsPages;

/// <summary>Public storefront lookup of a single Published CMS page by slug. Draft/Archived and
/// soft-deleted pages are treated as not found.</summary>
public record GetCmsPageBySlugQuery(string Slug) : IRequest<CmsPageDto>;

public class GetCmsPageBySlugQueryHandler : IRequestHandler<GetCmsPageBySlugQuery, CmsPageDto>
{
    private readonly IApplicationDbContext _db;

    public GetCmsPageBySlugQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsPageDto> Handle(GetCmsPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = (request.Slug ?? string.Empty).Trim();

        var page = await _db.CMSPages
            .AsNoTracking()
            .Include(p => p.PageGroup)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == CmsContentStatus.Published, cancellationToken)
            ?? throw new NotFoundException(nameof(CMSPage), slug);

        return CmsPageDto.From(page);
    }
}
