using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.SpecificationAttributes;

/// <summary>Returns a page of specification attributes ordered by display order then name, optionally filtered by a name search term.</summary>
public record ListSpecificationAttributesQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedList<SpecificationAttributeDto>>;

public class ListSpecificationAttributesQueryHandler : IRequestHandler<ListSpecificationAttributesQuery, PaginatedList<SpecificationAttributeDto>>
{
    private readonly IApplicationDbContext _db;

    public ListSpecificationAttributesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<SpecificationAttributeDto>> Handle(ListSpecificationAttributesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<SpecificationAttribute> query = _db.SpecificationAttributes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a => a.Name.Contains(term));
        }

        var ordered = query.OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name);

        var page = await PaginatedList<SpecificationAttribute>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(SpecificationAttributeDto.From).ToList();
        return new PaginatedList<SpecificationAttributeDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
