using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Subscriptions;

/// <summary>An admin cancels any customer's subscription on their behalf (WO-49 admin management note). Terminal.</summary>
public record AdminCancelSubscriptionCommand(Guid Id) : IRequest<SubscriptionDto>;

public class AdminCancelSubscriptionCommandHandler : IRequestHandler<AdminCancelSubscriptionCommand, SubscriptionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISubscriptionService _subscriptions;

    public AdminCancelSubscriptionCommandHandler(IApplicationDbContext db, ISubscriptionService subscriptions)
    {
        _db = db;
        _subscriptions = subscriptions;
    }

    public async Task<SubscriptionDto> Handle(AdminCancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await _subscriptions.CancelAsync(request.Id, cancellationToken);

        var subscription = await _db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Subscription), request.Id);

        return SubscriptionDto.From(subscription);
    }
}
