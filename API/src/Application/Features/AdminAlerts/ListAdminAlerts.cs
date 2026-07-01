using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.AdminAlerts;

public record ListAdminAlertsQuery(bool? UnresolvedOnly = null) : IRequest<IReadOnlyList<AdminAlertDto>>;

public class ListAdminAlertsQueryHandler
    : IRequestHandler<ListAdminAlertsQuery, IReadOnlyList<AdminAlertDto>>
{
    private readonly IApplicationDbContext _db;

    public ListAdminAlertsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminAlertDto>> Handle(ListAdminAlertsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AdminAlerts.AsNoTracking();

        if (request.UnresolvedOnly == true)
            query = query.Where(a => !a.IsResolved);

        var alerts = await query
            .OrderByDescending(a => a.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return alerts.Select(AdminAlertDto.From).ToList();
    }
}
