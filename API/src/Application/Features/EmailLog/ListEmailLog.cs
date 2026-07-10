using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.EmailLog;

/// <summary>
/// Lists queued/sent emails (newest first) for the admin Email Log, optionally filtered by
/// recipient/subject search, delivery status and/or notification category.
/// </summary>
public record ListEmailLogQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    EmailStatus? Status = null,
    NotificationCategory? Category = null) : IRequest<PaginatedList<EmailLogItemDto>>;

public class ListEmailLogQueryHandler : IRequestHandler<ListEmailLogQuery, PaginatedList<EmailLogItemDto>>
{
    private readonly IApplicationDbContext _db;

    public ListEmailLogQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<EmailLogItemDto>> Handle(ListEmailLogQuery request, CancellationToken cancellationToken)
    {
        var query = _db.EmailQueue.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(e =>
                e.RecipientEmail.Contains(term)
                || e.RenderedSubject.Contains(term)
                || (e.RecipientName != null && e.RecipientName.Contains(term)));
        }

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (request.Category.HasValue)
            query = query.Where(e => e.Category == request.Category.Value);

        var ordered = query.OrderByDescending(e => e.CreatedOnUtc);
        var page = await PaginatedList<EmailQueue>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(EmailLogItemDto.From).ToList();

        return new PaginatedList<EmailLogItemDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
