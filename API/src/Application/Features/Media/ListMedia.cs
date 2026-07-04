using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Media;

/// <summary>
/// The media library (WO-122): a page of committed media, searchable by SEO file name / alt text and
/// filterable by media type, newest first. Each row's public URL is resolved at read time.
/// </summary>
public record ListMediaQuery(int Page = 1, int PageSize = 24, string? Search = null, string? MediaType = null)
    : IRequest<PaginatedList<MediaDto>>;

public class ListMediaQueryHandler : IRequestHandler<ListMediaQuery, PaginatedList<MediaDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public ListMediaQueryHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<PaginatedList<MediaDto>> Handle(ListMediaQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Media.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(m => m.SeoFileName.Contains(term)
                || (m.AltText != null && m.AltText.Contains(term))
                || m.OriginalFileName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.MediaType)
            && Enum.TryParse<MediaType>(request.MediaType, ignoreCase: true, out var type))
        {
            query = query.Where(m => m.MediaType == type);
        }

        var ordered = query.OrderByDescending(m => m.CreatedOnUtc);
        var page = await PaginatedList<Domain.Entities.Media>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);

        var items = new List<MediaDto>(page.Items.Count);
        foreach (var m in page.Items)
        {
            var url = await _storage.GetFileUrlAsync(m.AssetKey, cancellationToken);
            items.Add(MediaDto.From(m, url));
        }

        return new PaginatedList<MediaDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
