using MediatR;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Wishlist;

/// <summary>
/// Moves a wishlist item into the current customer's cart and removes it from the wishlist (AC-CHK-002.2).
/// The cart add reuses the Cart feature so availability checks and price snapshotting are shared.
/// </summary>
public record MoveWishlistItemToCartCommand(Guid ItemId, int Quantity = 1) : IRequest<WishlistDto>;

public class MoveWishlistItemToCartCommandHandler : IRequestHandler<MoveWishlistItemToCartCommand, WishlistDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ISender _mediator;

    public MoveWishlistItemToCartCommandHandler(IApplicationDbContext db, ICurrentUserService current, ISender mediator)
    {
        _db = db;
        _current = current;
        _mediator = mediator;
    }

    public async Task<WishlistDto> Handle(MoveWishlistItemToCartCommand request, CancellationToken cancellationToken)
    {
        var wishlist = await WishlistResolver.ResolveExistingAsync(_db, _current, cancellationToken);

        var item = wishlist.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new NotFoundException(nameof(WishlistItem), request.ItemId);

        // Delegate to the Cart feature: validates the product/variant is purchasable and snapshots the
        // current price (AC-CHK-001.1). For an authenticated customer the session id is ignored.
        var quantity = request.Quantity < 1 ? 1 : request.Quantity;
        await _mediator.Send(
            new VSky.Application.Features.Cart.AddItemCommand(item.ProductId, item.ProductVariantId, quantity),
            cancellationToken);

        wishlist.Items.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);

        return await WishlistResolver.BuildDtoAsync(_db, wishlist, cancellationToken);
    }
}
