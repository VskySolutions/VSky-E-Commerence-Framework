using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Manufacturers;

/// <summary>Updates an existing manufacturer/brand.</summary>
public record UpdateManufacturerCommand(
    Guid Id,
    string Name,
    string? Description = null,
    string? LogoUrl = null,
    string? Slug = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? MetaKeywords = null,
    int DisplayOrder = 0,
    bool IsEnabled = true) : IRequest<ManufacturerDto>;

public class UpdateManufacturerCommandValidator : AbstractValidator<UpdateManufacturerCommand>
{
    public UpdateManufacturerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(220);
    }
}

public class UpdateManufacturerCommandHandler : IRequestHandler<UpdateManufacturerCommand, ManufacturerDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateManufacturerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ManufacturerDto> Handle(UpdateManufacturerCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Manufacturers
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Manufacturer), request.Id);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.LogoUrl = request.LogoUrl;
        entity.Slug = request.Slug;
        entity.MetaTitle = request.MetaTitle;
        entity.MetaDescription = request.MetaDescription;
        entity.MetaKeywords = request.MetaKeywords;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsEnabled = request.IsEnabled;

        await _db.SaveChangesAsync(cancellationToken);
        return ManufacturerDto.From(entity);
    }
}
