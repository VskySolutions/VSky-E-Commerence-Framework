using FluentValidation;
using MediatR;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.ProductAttributes;

/// <summary>An attribute value to create, or (when <see cref="Id"/> is supplied) update during reconciliation.</summary>
public record ProductAttributeValueInput(Guid? Id, string Value, int DisplayOrder);

/// <summary>Creates a product attribute together with its selectable values (AC-CAT-003.1).</summary>
public record CreateProductAttributeCommand(
    string Name,
    string? Description = null,
    int DisplayOrder = 0,
    List<ProductAttributeValueInput>? Values = null) : IRequest<ProductAttributeDto>;

public class CreateProductAttributeCommandValidator : AbstractValidator<CreateProductAttributeCommand>
{
    public CreateProductAttributeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleForEach(x => x.Values).ChildRules(v =>
        {
            v.RuleFor(i => i.Value).NotEmpty().MaximumLength(400);
        });
    }
}

public class CreateProductAttributeCommandHandler : IRequestHandler<CreateProductAttributeCommand, ProductAttributeDto>
{
    private readonly IApplicationDbContext _db;

    public CreateProductAttributeCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductAttributeDto> Handle(CreateProductAttributeCommand request, CancellationToken cancellationToken)
    {
        var entity = new ProductAttribute
        {
            Name = request.Name,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
        };

        foreach (var value in request.Values ?? new())
        {
            entity.Values.Add(new ProductAttributeValue
            {
                Value = value.Value,
                DisplayOrder = value.DisplayOrder,
            });
        }

        _db.ProductAttributes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ProductAttributeDto.From(entity);
    }
}
