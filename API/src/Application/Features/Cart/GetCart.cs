using MediatR;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Cart;

/// <summary>
/// Returns the caller's active cart, creating an empty one if none exists (REQ-CHK-001). For an
/// authenticated buyer this returns their persisted cart, restoring it on login (AC-CHK-001.4).
/// </summary>
public record GetCartQuery(string? SessionId = null) : IRequest<CartDto>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ICustomerGroupService _groups;

    public GetCartQueryHandler(IApplicationDbContext db, ICurrentUserService current, ICustomerGroupService groups)
    {
        _db = db;
        _current = current;
        _groups = groups;
    }

    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await CartResolver.ResolveOrCreateAsync(_db, _current, request.SessionId, cancellationToken);

        // Persists a newly created cart so it can be restored later; a no-op when the cart already existed.
        await _db.SaveChangesAsync(cancellationToken);

        return await CartResolver.BuildDtoAsync(_db, _groups, cart, cancellationToken);
    }
}
