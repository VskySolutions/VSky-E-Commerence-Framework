using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;

namespace VSky.Application.Features.Credentials;

/// <summary>Tests connectivity of a stored credential without exposing its value (AC-TEN-002.5).</summary>
public record TestCredentialCommand(string ServiceType) : IRequest<ConnectivityTestResult>;

public class TestCredentialCommandHandler : IRequestHandler<TestCredentialCommand, ConnectivityTestResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ICredentialVault _vault;
    private readonly ICredentialConnectivityChecker _checker;

    public TestCredentialCommandHandler(
        IApplicationDbContext db,
        ICredentialVault vault,
        ICredentialConnectivityChecker checker)
    {
        _db = db;
        _vault = vault;
        _checker = checker;
    }

    public async Task<ConnectivityTestResult> Handle(TestCredentialCommand request, CancellationToken cancellationToken)
    {
        var cred = await _db.TenantCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ServiceType == request.ServiceType, cancellationToken)
            ?? throw new NotFoundException("Credential", request.ServiceType);

        var plaintext = _vault.Decrypt(cred.EncryptedValue);
        return await _checker.TestAsync(request.ServiceType, plaintext, cancellationToken);
    }
}
