using FluentValidation;
using MediatR;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Cart;

/// <summary>
/// Sets the quantity of an existing cart line and returns the recalculated cart. A quantity of 0
/// removes the line. <see cref="SessionId"/> identifies a guest cart.
/// </summary>
public record UpdateItemQuantityCommand(
    Guid ItemId,
    int Quantity,
    string? SessionId = null) : IRequest<CartDto>;

public class UpdateItemQuantityCommandValidator : AbstractValidator<UpdateItemQuantityCommand>
{
    public UpdateItemQuantityCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}

public class UpdateItemQuantityCommandHandler : IRequestHandler<UpdateItemQuantityCommand, CartDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ICustomerGroupService _groups;

    public UpdateItemQuantityCommandHandler(IApplicationDbContext db, ICurrentUserService current, ICustomerGroupService groups)
    {
        _db = db;
        _current = current;
        _groups = groups;
    }

    public async Task<CartDto> Handle(UpdateItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var cart = await CartResolver.ResolveExistingAsync(_db, _current, request.SessionId, cancellationToken);

        var item = cart.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new NotFoundException(nameof(CartItem), request.ItemId);

        if (request.Quantity == 0)
        {
            cart.Items.Remove(item);
            _db.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await CartResolver.BuildDtoAsync(_db, _groups, cart, cancellationToken);
    }
}
