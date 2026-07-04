using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Integrations;

// --- Create -----------------------------------------------------------------

/// <summary>Adds a credential field definition to a provider (AC-TEN-002.3). FieldCode is unique per provider.</summary>
public record CreateCredentialDefinitionCommand(
    Guid ProviderId,
    string FieldName,
    string FieldCode,
    CredentialFieldType DataType,
    bool IsRequired,
    bool IsSecret,
    string? Placeholder,
    string? HelpText,
    int DisplayOrder) : IRequest<CredentialDefinitionDto>;

public class CreateCredentialDefinitionCommandValidator : AbstractValidator<CreateCredentialDefinitionCommand>
{
    public CreateCredentialDefinitionCommandValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.FieldName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FieldCode).NotEmpty().MaximumLength(50)
            .Matches("^[a-z0-9_]+$").WithMessage("Field code may contain only lowercase letters, digits, and underscores.");
        RuleFor(x => x.Placeholder).MaximumLength(200);
        RuleFor(x => x.HelpText).MaximumLength(500);
    }
}

public class CreateCredentialDefinitionCommandHandler : IRequestHandler<CreateCredentialDefinitionCommand, CredentialDefinitionDto>
{
    private readonly IApplicationDbContext _db;
    public CreateCredentialDefinitionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CredentialDefinitionDto> Handle(CreateCredentialDefinitionCommand request, CancellationToken cancellationToken)
    {
        var fieldCode = request.FieldCode.Trim().ToLowerInvariant();

        if (!await _db.IntegrationProviders.AnyAsync(p => p.Id == request.ProviderId, cancellationToken))
            throw new NotFoundException(nameof(IntegrationProvider), request.ProviderId);

        if (await _db.CredentialDefinitions.AnyAsync(d => d.ProviderId == request.ProviderId && d.FieldCode == fieldCode, cancellationToken))
            throw new ConflictException($"Field code '{fieldCode}' already exists for this provider.");

        var definition = new CredentialDefinition
        {
            ProviderId = request.ProviderId,
            FieldName = request.FieldName.Trim(),
            FieldCode = fieldCode,
            DataType = request.DataType,
            IsRequired = request.IsRequired,
            IsSecret = request.IsSecret,
            Placeholder = request.Placeholder,
            HelpText = request.HelpText,
            DisplayOrder = request.DisplayOrder,
        };

        _db.CredentialDefinitions.Add(definition);
        await _db.SaveChangesAsync(cancellationToken);

        return CredentialDefinitionDto.From(definition);
    }
}

// --- Update -----------------------------------------------------------------

/// <summary>Updates a definition's editable fields. FieldCode is immutable (it keys the stored value).</summary>
public record UpdateCredentialDefinitionCommand(
    Guid Id,
    string FieldName,
    CredentialFieldType DataType,
    bool IsRequired,
    bool IsSecret,
    string? Placeholder,
    string? HelpText,
    int DisplayOrder) : IRequest<CredentialDefinitionDto>;

public class UpdateCredentialDefinitionCommandValidator : AbstractValidator<UpdateCredentialDefinitionCommand>
{
    public UpdateCredentialDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FieldName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Placeholder).MaximumLength(200);
        RuleFor(x => x.HelpText).MaximumLength(500);
    }
}

public class UpdateCredentialDefinitionCommandHandler : IRequestHandler<UpdateCredentialDefinitionCommand, CredentialDefinitionDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateCredentialDefinitionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CredentialDefinitionDto> Handle(UpdateCredentialDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _db.CredentialDefinitions
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CredentialDefinition), request.Id);

        definition.FieldName = request.FieldName.Trim();
        definition.DataType = request.DataType;
        definition.IsRequired = request.IsRequired;
        definition.IsSecret = request.IsSecret;
        definition.Placeholder = request.Placeholder;
        definition.HelpText = request.HelpText;
        definition.DisplayOrder = request.DisplayOrder;

        // Keep stored values' secret snapshot consistent if the secrecy flag changed.
        var values = await _db.IntegrationCredentials.Where(c => c.DefinitionId == definition.Id).ToListAsync(cancellationToken);
        foreach (var v in values)
            v.IsSecret = definition.IsSecret;

        await _db.SaveChangesAsync(cancellationToken);
        return CredentialDefinitionDto.From(definition);
    }
}

// --- Delete -----------------------------------------------------------------

/// <summary>Removes a definition and any stored values for it (the value FK is NoAction, so clear first).</summary>
public record DeleteCredentialDefinitionCommand(Guid Id) : IRequest;

public class DeleteCredentialDefinitionCommandHandler : IRequestHandler<DeleteCredentialDefinitionCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteCredentialDefinitionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCredentialDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _db.CredentialDefinitions
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CredentialDefinition), request.Id);

        var values = await _db.IntegrationCredentials.Where(c => c.DefinitionId == definition.Id).ToListAsync(cancellationToken);
        _db.IntegrationCredentials.RemoveRange(values);
        _db.CredentialDefinitions.Remove(definition);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
