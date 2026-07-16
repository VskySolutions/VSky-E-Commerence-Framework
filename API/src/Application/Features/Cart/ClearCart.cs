using MediatR;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Cart;

/// <summary>
/// Empties the caller's cart and returns it. Idempotent: an empty cart is returned even when none
/// existed. <see cref="SessionId"/> identifies a guest cart.
/// </summary>
public record ClearCartCommand(string? SessionId = null) : IRequest<CartDto>;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, CartDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ICustomerGroupService _groups;

    public ClearCartCommandHandler(IApplicationDbContext db, ICurrentUserService current, ICustomerGroupService groups)
    {
        _db = db;
        _current = current;
        _groups = groups;
    }

    public async Task<CartDto> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await CartResolver.ResolveOrCreateAsync(_db, _current, request.SessionId, cancellationToken);

        if (cart.Items.Count > 0)
        {
            var toRemove = cart.Items.ToList();
            _db.CartItems.RemoveRange(toRemove);
            cart.Items.Clear();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await CartResolver.BuildDtoAsync(_db, _groups, cart, cancellationToken);
    }
}
