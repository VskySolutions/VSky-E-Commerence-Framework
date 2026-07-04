using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Products;

/// <summary>A product picture: a Media asset assigned to a product, with its resolved public URL (WO-123).</summary>
public class ProductPictureDto
{
    public Guid Id { get; set; }
    public Guid MediaId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
}

// ---- List --------------------------------------------------------------------

/// <summary>Lists a product's Media-backed pictures (ordered), each with its resolved public URL (WO-123).</summary>
public record ListProductPicturesQuery(Guid ProductId) : IRequest<List<ProductPictureDto>>;

public class ListProductPicturesQueryHandler : IRequestHandler<ListProductPicturesQuery, List<ProductPictureDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public ListProductPicturesQueryHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<List<ProductPictureDto>> Handle(ListProductPicturesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _db.ProductPictures.AsNoTracking()
            .Where(p => p.ProductId == request.ProductId)
            .Include(p => p.Media)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);

        var result = new List<ProductPictureDto>(rows.Count);
        foreach (var p in rows)
        {
            var url = p.Media is null ? string.Empty : await _storage.GetFileUrlAsync(p.Media.AssetKey, cancellationToken);
            result.Add(new ProductPictureDto
            {
                Id = p.Id,
                MediaId = p.MediaId,
                Url = url,
                AltText = p.Media?.AltText,
                DisplayOrder = p.DisplayOrder,
            });
        }
        return result;
    }
}

// ---- Assign ------------------------------------------------------------------

/// <summary>Assigns a committed Media asset to a product as a picture (AC-CAT-012.1); returns the created row (WO-123).</summary>
public record AssignProductPictureCommand(Guid ProductId, Guid MediaId, int DisplayOrder = 0) : IRequest<ProductPictureDto>;

public class AssignProductPictureCommandValidator : AbstractValidator<AssignProductPictureCommand>
{
    public AssignProductPictureCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.MediaId).NotEmpty();
    }
}

public class AssignProductPictureCommandHandler : IRequestHandler<AssignProductPictureCommand, ProductPictureDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public AssignProductPictureCommandHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<ProductPictureDto> Handle(AssignProductPictureCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken))
            throw new NotFoundException(nameof(Product), request.ProductId);

        var media = await _db.Media.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.MediaId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Media), request.MediaId);

        // Append after the current last picture when no explicit order is supplied.
        var order = request.DisplayOrder;
        if (order == 0)
        {
            var max = await _db.ProductPictures.Where(p => p.ProductId == request.ProductId)
                .Select(p => (int?)p.DisplayOrder).MaxAsync(cancellationToken) ?? -1;
            order = max + 1;
        }

        var picture = new ProductPicture { ProductId = request.ProductId, MediaId = request.MediaId, DisplayOrder = order };
        _db.ProductPictures.Add(picture);
        await _db.SaveChangesAsync(cancellationToken);

        return new ProductPictureDto
        {
            Id = picture.Id,
            MediaId = media.Id,
            Url = await _storage.GetFileUrlAsync(media.AssetKey, cancellationToken),
            AltText = media.AltText,
            DisplayOrder = picture.DisplayOrder,
        };
    }
}

// ---- Remove ------------------------------------------------------------------

/// <summary>Removes a product picture (the underlying Media asset is left intact) (WO-123).</summary>
public record RemoveProductPictureCommand(Guid PictureId) : IRequest;

public class RemoveProductPictureCommandHandler : IRequestHandler<RemoveProductPictureCommand>
{
    private readonly IApplicationDbContext _db;

    public RemoveProductPictureCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(RemoveProductPictureCommand request, CancellationToken cancellationToken)
    {
        var picture = await _db.ProductPictures.FirstOrDefaultAsync(p => p.Id == request.PictureId, cancellationToken);
        if (picture is null)
            return;

        _db.ProductPictures.Remove(picture);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
