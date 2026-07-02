using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.ProductAttributes;

/// <summary>Updates a product attribute and reconciles its values: adds new, updates existing by id, removes the rest (AC-CAT-003.1).</summary>
public record UpdateProductAttributeCommand(
    Guid Id,
    string Name,
    string? Description = null,
    int DisplayOrder = 0,
    List<ProductAttributeValueInput>? Values = null) : IRequest<ProductAttributeDto>;

public class UpdateProductAttributeCommandValidator : AbstractValidator<UpdateProductAttributeCommand>
{
    public UpdateProductAttributeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleForEach(x => x.Values).ChildRules(v =>
        {
            v.RuleFor(i => i.Value).NotEmpty().MaximumLength(400);
        });
    }
}

public class UpdateProductAttributeCommandHandler : IRequestHandler<UpdateProductAttributeCommand, ProductAttributeDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateProductAttributeCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductAttributeDto> Handle(UpdateProductAttributeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ProductAttributes
            .Include(a => a.Values)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductAttribute), request.Id);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.DisplayOrder = request.DisplayOrder;

        var inputs = request.Values ?? new();
        var keptIds = inputs
            .Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty)
            .Select(i => i.Id!.Value)
            .ToHashSet();

        foreach (var existing in entity.Values.Where(v => !keptIds.Contains(v.Id)).ToList())
        {
            entity.Values.Remove(existing);
            _db.ProductAttributeValues.Remove(existing);
        }

        foreach (var input in inputs)
        {
            var existing = input.Id is { } id && id != Guid.Empty
                ? entity.Values.FirstOrDefault(v => v.Id == id)
                : null;

            if (existing is null)
            {
                entity.Values.Add(new ProductAttributeValue
                {
                    Value = input.Value,
                    DisplayOrder = input.DisplayOrder,
                });
            }
            else
            {
                existing.Value = input.Value;
                existing.DisplayOrder = input.DisplayOrder;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ProductAttributeDto.From(entity);
    }
}
