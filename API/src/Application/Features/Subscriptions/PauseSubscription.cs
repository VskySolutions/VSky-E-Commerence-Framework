using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Subscriptions;

/// <summary>The authenticated buyer pauses one of their own subscriptions (AC-ORD-005.4).</summary>
public record PauseSubscriptionCommand(Guid Id) : IRequest<SubscriptionDto>;

public class PauseSubscriptionCommandHandler : IRequestHandler<PauseSubscriptionCommand, SubscriptionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISubscriptionService _subscriptions;
    private readonly ICurrentUserService _current;

    public PauseSubscriptionCommandHandler(
        IApplicationDbContext db, ISubscriptionService subscriptions, ICurrentUserService current)
    {
        _db = db;
        _subscriptions = subscriptions;
        _current = current;
    }

    public async Task<SubscriptionDto> Handle(PauseSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await SubscriptionOwnership.EnsureOwnedAsync(_db, _current, request.Id, cancellationToken);

        await _subscriptions.PauseAsync(request.Id, cancellationToken);

        return await SubscriptionOwnership.LoadDtoAsync(_db, request.Id, cancellationToken);
    }
}
