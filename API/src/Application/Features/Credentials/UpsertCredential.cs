using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Credentials;

/// <summary>Creates or replaces the credential for a service type, encrypting the value at rest.</summary>
public record UpsertCredentialCommand(string ServiceType, string Value, string? Description)
    : IRequest<CredentialSummaryDto>;

public class UpsertCredentialCommandValidator : AbstractValidator<UpsertCredentialCommand>
{
    public UpsertCredentialCommandValidator()
    {
        RuleFor(x => x.ServiceType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Value).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpsertCredentialCommandHandler : IRequestHandler<UpsertCredentialCommand, CredentialSummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICredentialVault _vault;
    private readonly IDateTimeProvider _clock;

    public UpsertCredentialCommandHandler(IApplicationDbContext db, ICredentialVault vault, IDateTimeProvider clock)
    {
        _db = db;
        _vault = vault;
        _clock = clock;
    }

    public async Task<CredentialSummaryDto> Handle(UpsertCredentialCommand request, CancellationToken cancellationToken)
    {
        var serviceType = request.ServiceType.Trim();
        var value = request.Value;

        var entity = await _db.TenantCredentials
            .FirstOrDefaultAsync(c => c.ServiceType == serviceType, cancellationToken);

        if (entity is null)
        {
            entity = new TenantCredential { ServiceType = serviceType, CreatedOnUtc = _clock.UtcNow };
            _db.TenantCredentials.Add(entity);
        }

        entity.EncryptedValue = _vault.Encrypt(value);
        entity.LastFourChars = value.Length >= 4 ? value[^4..] : value;
        entity.Description = request.Description;
        entity.UpdatedOnUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return CredentialSummaryDto.From(entity);
    }
}
