using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Webhooks;

// ---- Create ------------------------------------------------------------------

/// <summary>Registers a webhook endpoint and returns its generated signing secret once (AC-PLT-003.1).</summary>
public record CreateWebhookSubscriptionCommand(string Url, List<string> EventTypes, string? Description = null)
    : IRequest<WebhookSubscriptionDto>;

public class CreateWebhookSubscriptionCommandValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    public CreateWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid absolute http(s) URL is required.");
        RuleFor(x => x.EventTypes).NotEmpty();
        RuleForEach(x => x.EventTypes).NotEmpty().MaximumLength(128);
    }
}

public class CreateWebhookSubscriptionCommandHandler : IRequestHandler<CreateWebhookSubscriptionCommand, WebhookSubscriptionDto>
{
    private readonly IApplicationDbContext _db;
    public CreateWebhookSubscriptionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<WebhookSubscriptionDto> Handle(CreateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = new WebhookSubscription
        {
            Url = request.Url,
            Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            IsActive = true,
            Description = request.Description,
        };

        foreach (var eventType in request.EventTypes.Select(e => e.Trim()).Where(e => e.Length > 0).Distinct())
            subscription.Events.Add(new WebhookSubscriptionEvent { EventType = eventType });

        _db.WebhookSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        // The secret is shown exactly once, on creation.
        return WebhookSubscriptionDto.From(subscription, includeSecret: true);
    }
}

// ---- List --------------------------------------------------------------------

/// <summary>Lists registered webhook endpoints (secrets are never returned) (AC-PLT-003.4).</summary>
public record ListWebhookSubscriptionsQuery : IRequest<List<WebhookSubscriptionDto>>;

public class ListWebhookSubscriptionsQueryHandler : IRequestHandler<ListWebhookSubscriptionsQuery, List<WebhookSubscriptionDto>>
{
    private readonly IApplicationDbContext _db;
    public ListWebhookSubscriptionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<WebhookSubscriptionDto>> Handle(ListWebhookSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var subs = await _db.WebhookSubscriptions
            .AsNoTracking()
            .Include(s => s.Events)
            .OrderByDescending(s => s.CreatedOnUtc)
            .ToListAsync(cancellationToken);
        return subs.Select(s => WebhookSubscriptionDto.From(s)).ToList();
    }
}

// ---- Delete ------------------------------------------------------------------

/// <summary>Removes a webhook endpoint (soft delete; idempotent).</summary>
public record DeleteWebhookSubscriptionCommand(Guid Id) : IRequest;

public class DeleteWebhookSubscriptionCommandHandler : IRequestHandler<DeleteWebhookSubscriptionCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteWebhookSubscriptionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _db.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (subscription is null)
            return;

        _db.WebhookSubscriptions.Remove(subscription);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---- Deliveries --------------------------------------------------------------

/// <summary>Lists delivery history with response status + attempt count (AC-PLT-003.4).</summary>
public record ListWebhookDeliveriesQuery(Guid? SubscriptionId = null, int Page = 1, int PageSize = 50)
    : IRequest<PaginatedList<WebhookDeliveryDto>>;

public class ListWebhookDeliveriesQueryHandler : IRequestHandler<ListWebhookDeliveriesQuery, PaginatedList<WebhookDeliveryDto>>
{
    private readonly IApplicationDbContext _db;
    public ListWebhookDeliveriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<WebhookDeliveryDto>> Handle(ListWebhookDeliveriesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<WebhookDelivery> query = _db.WebhookDeliveries.AsNoTracking();
        if (request.SubscriptionId is Guid sid)
            query = query.Where(d => d.SubscriptionId == sid);

        var page = await PaginatedList<WebhookDelivery>.CreateAsync(
            query.OrderByDescending(d => d.OccurredAtUtc), request.Page, request.PageSize, cancellationToken);
        return new PaginatedList<WebhookDeliveryDto>(page.Items.Select(WebhookDeliveryDto.From).ToList(), page.TotalCount, page.PageNumber, page.PageSize);
    }
}
