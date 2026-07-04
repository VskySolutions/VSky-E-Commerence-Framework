using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Integrations;

/// <summary>
/// Creates/updates/clears a provider's credential values in one call (the form save). Secret fields are
/// encrypted at rest via the Credential Vault; a whitespace value clears the stored field. Every change is
/// recorded in the audit trail with masked secret values (AC-TEN-002.5/002.9). Returns the refreshed form.
/// </summary>
public record SaveIntegrationCredentialsCommand(Guid ProviderId, Dictionary<string, string?> Values)
    : IRequest<ProviderFormDto>;

public class SaveIntegrationCredentialsCommandValidator : AbstractValidator<SaveIntegrationCredentialsCommand>
{
    public SaveIntegrationCredentialsCommandValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.Values).NotNull();
    }
}

public class SaveIntegrationCredentialsCommandHandler : IRequestHandler<SaveIntegrationCredentialsCommand, ProviderFormDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICredentialVault _vault;
    private readonly ICurrentUserService _current;
    private readonly IDateTimeProvider _clock;

    public SaveIntegrationCredentialsCommandHandler(
        IApplicationDbContext db, ICredentialVault vault, ICurrentUserService current, IDateTimeProvider clock)
    {
        _db = db;
        _vault = vault;
        _current = current;
        _clock = clock;
    }

    public async Task<ProviderFormDto> Handle(SaveIntegrationCredentialsCommand request, CancellationToken cancellationToken)
    {
        var provider = await _db.IntegrationProviders
            .Include(p => p.Definitions)
            .Include(p => p.Credentials)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationProvider), request.ProviderId);

        var definitionsByCode = provider.Definitions.ToDictionary(d => d.FieldCode, StringComparer.OrdinalIgnoreCase);
        var auditedFields = new Dictionary<string, string>();

        foreach (var (rawCode, rawValue) in request.Values)
        {
            if (!definitionsByCode.TryGetValue(rawCode.Trim(), out var definition))
                continue; // ignore fields not defined for this provider

            var existing = provider.Credentials.FirstOrDefault(c => c.DefinitionId == definition.Id);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (existing is not null)
                {
                    _db.IntegrationCredentials.Remove(existing);
                    provider.Credentials.Remove(existing);
                    auditedFields[definition.FieldCode] = "(cleared)";
                }
                continue;
            }

            var value = rawValue;
            var lastFour = value.Length >= 4 ? value[^4..] : value;

            if (existing is null)
            {
                existing = new IntegrationCredential { ProviderId = provider.Id, DefinitionId = definition.Id };
                _db.IntegrationCredentials.Add(existing);
                provider.Credentials.Add(existing);
            }

            existing.IsSecret = definition.IsSecret;
            if (definition.IsSecret)
            {
                existing.Value = _vault.Encrypt(value);
                existing.LastFourChars = lastFour;
                auditedFields[definition.FieldCode] = CredentialMasking.Mask(lastFour);
            }
            else
            {
                existing.Value = value;
                existing.LastFourChars = null;
                auditedFields[definition.FieldCode] = value;
            }
        }

        if (auditedFields.Count > 0)
        {
            _db.AuditTrails.Add(new AuditTrail
            {
                UserId = _current.UserId,
                ActorName = _current.Email,
                Action = "IntegrationCredential.Save",
                EntityType = nameof(IntegrationProvider),
                EntityId = provider.Id.ToString(),
                CorrelationId = _current.CorrelationId,
                IpAddress = _current.IpAddress,
                MetadataJson = JsonSerializer.Serialize(new { provider.Code, fields = auditedFields }),
                TimestampUtc = _clock.UtcNow,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await LoadFormAsync(provider.Id, cancellationToken);
    }

    private async Task<ProviderFormDto> LoadFormAsync(Guid providerId, CancellationToken ct)
    {
        var provider = await _db.IntegrationProviders
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Definitions)
            .Include(p => p.Credentials)
            .AsSplitQuery()
            .FirstAsync(p => p.Id == providerId, ct);

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

/// <summary>Clears a single stored credential field for a provider, with an audit record.</summary>
public record DeleteIntegrationCredentialCommand(Guid ProviderId, string FieldCode) : IRequest;

public class DeleteIntegrationCredentialCommandHandler : IRequestHandler<DeleteIntegrationCredentialCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IDateTimeProvider _clock;

    public DeleteIntegrationCredentialCommandHandler(IApplicationDbContext db, ICurrentUserService current, IDateTimeProvider clock)
    {
        _db = db;
        _current = current;
        _clock = clock;
    }

    public async Task Handle(DeleteIntegrationCredentialCommand request, CancellationToken cancellationToken)
    {
        var credential = await _db.IntegrationCredentials
            .Include(c => c.Definition)
            .FirstOrDefaultAsync(c => c.ProviderId == request.ProviderId && c.Definition!.FieldCode == request.FieldCode, cancellationToken);

        if (credential is null)
            return; // already absent — idempotent

        _db.IntegrationCredentials.Remove(credential);
        _db.AuditTrails.Add(new AuditTrail
        {
            UserId = _current.UserId,
            ActorName = _current.Email,
            Action = "IntegrationCredential.Delete",
            EntityType = nameof(IntegrationProvider),
            EntityId = request.ProviderId.ToString(),
            CorrelationId = _current.CorrelationId,
            IpAddress = _current.IpAddress,
            MetadataJson = JsonSerializer.Serialize(new { request.FieldCode }),
            TimestampUtc = _clock.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
