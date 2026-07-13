using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.AdminAlerts;

/// <summary>Paged admin-alert history (newest first), filterable by free-text search, severity and resolved state.</summary>
public record ListAdminAlertsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Severity = null,
    bool? Resolved = null) : IRequest<PaginatedList<AdminAlertDto>>;

public class ListAdminAlertsQueryHandler
    : IRequestHandler<ListAdminAlertsQuery, PaginatedList<AdminAlertDto>>
{
    private readonly IApplicationDbContext _db;

    public ListAdminAlertsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<AdminAlertDto>> Handle(ListAdminAlertsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AdminAlerts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a =>
                a.Title.Contains(term)
                || a.AlertType.Contains(term)
                || (a.Message != null && a.Message.Contains(term))
                || (a.Source != null && a.Source.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Severity))
            query = query.Where(a => a.Severity == request.Severity);

        if (request.Resolved.HasValue)
            query = query.Where(a => a.IsResolved == request.Resolved.Value);

        var ordered = query.OrderByDescending(a => a.CreatedOnUtc);
        var page = await PaginatedList<AdminAlert>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(AdminAlertDto.From).ToList();
        return new PaginatedList<AdminAlertDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
