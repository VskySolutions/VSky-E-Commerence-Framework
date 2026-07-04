using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Integrations;

/// <summary>
/// Probes connectivity for a provider using its stored credentials, without exposing any value. Reuses the
/// per-provider connectivity checker keyed by provider code (CredentialConnectivityChecker).
/// </summary>
public record TestIntegrationProviderCommand(Guid ProviderId) : IRequest<ConnectivityTestResult>;

public class TestIntegrationProviderCommandHandler : IRequestHandler<TestIntegrationProviderCommand, ConnectivityTestResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ICredentialVault _vault;
    private readonly ICredentialConnectivityChecker _checker;

    public TestIntegrationProviderCommandHandler(
        IApplicationDbContext db, ICredentialVault vault, ICredentialConnectivityChecker checker)
    {
        _db = db;
        _vault = vault;
        _checker = checker;
    }

    public async Task<ConnectivityTestResult> Handle(TestIntegrationProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await _db.IntegrationProviders
            .AsNoTracking()
            .Include(p => p.Definitions)
            .Include(p => p.Credentials)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationProvider), request.ProviderId);

        // Prefer a secret field's value (the API key/token) for the probe; fall back to any stored value.
        var secretDefinitionIds = provider.Definitions.Where(d => d.IsSecret).Select(d => d.Id).ToHashSet();
        var chosen = provider.Credentials.FirstOrDefault(c => secretDefinitionIds.Contains(c.DefinitionId))
                     ?? provider.Credentials.FirstOrDefault();

        var plaintext = chosen is null
            ? string.Empty
            : chosen.IsSecret ? _vault.Decrypt(chosen.Value) : chosen.Value;

        return await _checker.TestAsync(provider.Code, plaintext, cancellationToken);
    }
}
