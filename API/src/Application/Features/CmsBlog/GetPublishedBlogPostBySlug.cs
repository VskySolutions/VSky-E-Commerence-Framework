using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CmsBlog;

/// <summary>Public storefront lookup of a single Published blog post by slug. Draft/Archived and
/// soft-deleted posts are treated as not found.</summary>
public record GetPublishedBlogPostBySlugQuery(string Slug) : IRequest<CmsBlogPostDto>;

public class GetPublishedBlogPostBySlugQueryHandler : IRequestHandler<GetPublishedBlogPostBySlugQuery, CmsBlogPostDto>
{
    private readonly IApplicationDbContext _db;

    public GetPublishedBlogPostBySlugQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CmsBlogPostDto> Handle(GetPublishedBlogPostBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = (request.Slug ?? string.Empty).Trim();

        var post = await _db.CMSBlogPosts
            .AsNoTracking()
            .Include(p => p.FeaturedImageMedia)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == CmsContentStatus.Published, cancellationToken)
            ?? throw new NotFoundException(nameof(CMSBlogPost), slug);

        return CmsBlogPostDto.From(post);
    }
}
