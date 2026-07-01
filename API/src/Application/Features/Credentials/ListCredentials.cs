using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Credentials;

public record ListCredentialsQuery : IRequest<IReadOnlyList<CredentialSummaryDto>>;

public class ListCredentialsQueryHandler
    : IRequestHandler<ListCredentialsQuery, IReadOnlyList<CredentialSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public ListCredentialsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CredentialSummaryDto>> Handle(ListCredentialsQuery request, CancellationToken cancellationToken)
    {
        var creds = await _db.TenantCredentials
            .AsNoTracking()
            .OrderBy(c => c.ServiceType)
            .ToListAsync(cancellationToken);

        return creds.Select(CredentialSummaryDto.From).ToList();
    }
}
