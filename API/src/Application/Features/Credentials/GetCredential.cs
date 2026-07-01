using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Credentials;

public record GetCredentialQuery(string ServiceType) : IRequest<CredentialSummaryDto>;

public class GetCredentialQueryHandler : IRequestHandler<GetCredentialQuery, CredentialSummaryDto>
{
    private readonly IApplicationDbContext _db;

    public GetCredentialQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CredentialSummaryDto> Handle(GetCredentialQuery request, CancellationToken cancellationToken)
    {
        var cred = await _db.TenantCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ServiceType == request.ServiceType, cancellationToken)
            ?? throw new NotFoundException("Credential", request.ServiceType);

        return CredentialSummaryDto.From(cred);
    }
}
