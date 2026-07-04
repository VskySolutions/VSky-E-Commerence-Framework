using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Integrations;

/// <summary>
/// Lists integration providers grouped by category, with optional search/filter (AC-TEN-002.1/002.8).
/// Values are never included here — this is the catalogue/status view.
/// </summary>
public record ListIntegrationsQuery(string? Search = null, string? CategoryCode = null, bool? IsEnabled = null)
    : IRequest<IReadOnlyList<IntegrationCategoryDto>>;

public class ListIntegrationsQueryHandler : IRequestHandler<ListIntegrationsQuery, IReadOnlyList<IntegrationCategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public ListIntegrationsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<IntegrationCategoryDto>> Handle(ListIntegrationsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.IntegrationProviders
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Definitions)
            .Include(p => p.Credentials)
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%") || EF.Functions.Like(p.Code, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryCode))
            query = query.Where(p => p.Category!.Code == request.CategoryCode);

        if (request.IsEnabled.HasValue)
            query = query.Where(p => p.IsEnabled == request.IsEnabled.Value);

        var providers = await query.ToListAsync(cancellationToken);
        var summariesByCategory = providers
            .Select(IntegrationProviderSummaryDto.From)
            .GroupBy(s => s.CategoryId)
            .ToDictionary(g => g.Key, g => (IEnumerable<IntegrationProviderSummaryDto>)g.ToList());

        var categories = await _db.IntegrationCategories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories
            .Where(c => summariesByCategory.ContainsKey(c.Id))
            .Select(c => IntegrationCategoryDto.From(c, summariesByCategory[c.Id]))
            .ToList();
    }
}
