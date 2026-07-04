using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Integrations;

// --- Create -----------------------------------------------------------------

/// <summary>Adds a provider to a category. Code is the stable runtime key and must be unique (AC-TEN-002.2).</summary>
public record CreateIntegrationProviderCommand(
    Guid CategoryId,
    string Name,
    string Code,
    string? Description,
    bool IsEnabled,
    int DisplayOrder) : IRequest<IntegrationProviderSummaryDto>;

public class CreateIntegrationProviderCommandValidator : AbstractValidator<CreateIntegrationProviderCommand>
{
    public CreateIntegrationProviderCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[a-z0-9-]+$").WithMessage("Code may contain only lowercase letters, digits, and hyphens.");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateIntegrationProviderCommandHandler : IRequestHandler<CreateIntegrationProviderCommand, IntegrationProviderSummaryDto>
{
    private readonly IApplicationDbContext _db;
    public CreateIntegrationProviderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<IntegrationProviderSummaryDto> Handle(CreateIntegrationProviderCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToLowerInvariant();

        var category = await _db.IntegrationCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationCategory), request.CategoryId);

        if (await _db.IntegrationProviders.AnyAsync(p => p.Code == code, cancellationToken))
            throw new ConflictException($"A provider with code '{code}' already exists.");

        var provider = new IntegrationProvider
        {
            CategoryId = category.Id,
            Category = category,
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description,
            IsEnabled = request.IsEnabled,
            DisplayOrder = request.DisplayOrder,
        };

        _db.IntegrationProviders.Add(provider);
        await _db.SaveChangesAsync(cancellationToken);

        return IntegrationProviderSummaryDto.From(provider);
    }
}

// --- Update -----------------------------------------------------------------

/// <summary>Updates a provider's editable fields. Code is immutable (it is the runtime lookup key).</summary>
public record UpdateIntegrationProviderCommand(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    bool IsEnabled,
    int DisplayOrder) : IRequest<IntegrationProviderSummaryDto>;

public class UpdateIntegrationProviderCommandValidator : AbstractValidator<UpdateIntegrationProviderCommand>
{
    public UpdateIntegrationProviderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateIntegrationProviderCommandHandler : IRequestHandler<UpdateIntegrationProviderCommand, IntegrationProviderSummaryDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateIntegrationProviderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<IntegrationProviderSummaryDto> Handle(UpdateIntegrationProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await _db.IntegrationProviders
            .Include(p => p.Category)
            .Include(p => p.Definitions)
            .Include(p => p.Credentials)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationProvider), request.Id);

        if (provider.CategoryId != request.CategoryId)
        {
            var category = await _db.IntegrationCategories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
                ?? throw new NotFoundException(nameof(IntegrationCategory), request.CategoryId);
            provider.CategoryId = category.Id;
            provider.Category = category;
        }

        provider.Name = request.Name.Trim();
        provider.Description = request.Description;
        provider.IsEnabled = request.IsEnabled;
        provider.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync(cancellationToken);
        return IntegrationProviderSummaryDto.From(provider);
    }
}

// --- Delete -----------------------------------------------------------------

/// <summary>Retires a provider (soft delete); its code becomes reusable.</summary>
public record DeleteIntegrationProviderCommand(Guid Id) : IRequest;

public class DeleteIntegrationProviderCommandHandler : IRequestHandler<DeleteIntegrationProviderCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteIntegrationProviderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteIntegrationProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await _db.IntegrationProviders
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IntegrationProvider), request.Id);

        _db.IntegrationProviders.Remove(provider); // soft-deleted by SaveChanges (ISoftDeletable)
        await _db.SaveChangesAsync(cancellationToken);
    }
}
