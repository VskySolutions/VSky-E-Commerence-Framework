using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CmsBlog;

/// <summary>Public storefront blog list: Published posts only, newest first by publish date.</summary>
public record ListPublishedBlogPostsQuery(int Page = 1, int PageSize = 12) : IRequest<PaginatedList<CmsBlogPostDto>>;

public class ListPublishedBlogPostsQueryHandler : IRequestHandler<ListPublishedBlogPostsQuery, PaginatedList<CmsBlogPostDto>>
{
    private readonly IApplicationDbContext _db;

    public ListPublishedBlogPostsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<CmsBlogPostDto>> Handle(ListPublishedBlogPostsQuery request, CancellationToken cancellationToken)
    {
        var ordered = _db.CMSBlogPosts
            .AsNoTracking()
            .Include(p => p.FeaturedImageMedia)
            .Where(p => p.Status == CmsContentStatus.Published)
            .OrderByDescending(p => p.PublishedOnUtc)
            .ThenByDescending(p => p.CreatedOnUtc);

        var page = await PaginatedList<CMSBlogPost>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(CmsBlogPostDto.From).ToList();
        return new PaginatedList<CmsBlogPostDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
