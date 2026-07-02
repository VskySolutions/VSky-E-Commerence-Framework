using MediatR;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Cart;

/// <summary>Removes a single line from the caller's cart. <see cref="SessionId"/> identifies a guest cart.</summary>
public record RemoveItemCommand(Guid ItemId, string? SessionId = null) : IRequest<CartDto>;

public class RemoveItemCommandHandler : IRequestHandler<RemoveItemCommand, CartDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public RemoveItemCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<CartDto> Handle(RemoveItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await CartResolver.ResolveExistingAsync(_db, _current, request.SessionId, cancellationToken);

        var item = cart.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new NotFoundException(nameof(CartItem), request.ItemId);

        cart.Items.Remove(item);
        _db.CartItems.Remove(item);

        await _db.SaveChangesAsync(cancellationToken);
        return await CartResolver.BuildDtoAsync(_db, cart, cancellationToken);
    }
}
