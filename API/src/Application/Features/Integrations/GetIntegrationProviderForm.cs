using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Integrations;

/// <summary>
/// Returns the auto-generated credential form for a provider: its field definitions plus current values,
/// with secret values masked to the last four chars (AC-TEN-002.4/002.6/002.7).
/// </summary>
public record GetIntegrationProviderFormQuery(Guid ProviderId) : IRequest<ProviderFormDto>;

public class GetIntegrationProviderFormQueryHandler : IRequestHandler<GetIntegrationProviderFormQuery, ProviderFormDto>
{
    private readonly IApplicationDbContext _db;

    public GetIntegrationProviderFormQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProviderFormDto> Handle(GetIntegrationProviderFormQuery request, CancellationToken cancellationToken)
    {
        var provider = await _db.IntegrationProviders
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Definitions)
            .Include(p => p.Credentials)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationProvider), request.ProviderId);

        var valuesByDefinition = provider.Credentials.ToDictionary(c => c.DefinitionId);

        var fields = provider.Definitions
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.FieldName)
            .Select(d => ProviderFormFieldDto.From(d, valuesByDefinition.GetValueOrDefault(d.Id)))
            .ToList();

        return new ProviderFormDto
        {
            Provider = IntegrationProviderSummaryDto.From(provider),
            Fields = fields,
        };
    }
}
