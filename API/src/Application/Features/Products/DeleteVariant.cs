using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Products;

/// <summary>Soft-deletes a product variant (idempotent).</summary>
public record DeleteVariantCommand(Guid VariantId) : IRequest;

public class DeleteVariantCommandHandler : IRequestHandler<DeleteVariantCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteVariantCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken);

        if (variant is null)
            return;

        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
