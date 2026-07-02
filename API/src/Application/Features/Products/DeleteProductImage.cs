using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Products;

/// <summary>Deletes a product/variant gallery entry (idempotent).</summary>
public record DeleteProductImageCommand(Guid ImageId) : IRequest;

public class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteProductImageCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _db.ProductImages
            .FirstOrDefaultAsync(i => i.Id == request.ImageId, cancellationToken);

        if (image is null)
            return;

        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
