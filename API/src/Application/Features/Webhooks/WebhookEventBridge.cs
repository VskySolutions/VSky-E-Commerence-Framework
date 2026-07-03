using MediatR;
using Microsoft.Extensions.Logging;
using VSky.Application.Common.Interfaces;
using VSky.Application.Features.Checkout;
using VSky.Application.Features.OrderRouting;

namespace VSky.Application.Features.Webhooks;

/// <summary>The domain event-type keys webhook subscriptions can subscribe to.</summary>
public static class WebhookEventTypes
{
    public const string OrderPlaced = "order.placed";
    public const string OrderRouted = "order.routed";
    public const string OrderUnroutable = "order.unroutable";

    public static readonly IReadOnlyList<string> All = new[] { OrderPlaced, OrderRouted, OrderUnroutable };
}

/// <summary>
/// Bridges existing MediatR domain notifications onto the webhook <see cref="IDomainEventBus"/> so WO-5
/// stays decoupled from the features that raise them. Best-effort: a bus failure never breaks the
/// originating flow.
/// </summary>
public class WebhookEventBridge :
    INotificationHandler<OrderPlaced>,
    INotificationHandler<OrderRouted>,
    INotificationHandler<OrderUnroutable>
{
    private readonly IDomainEventBus _bus;
    private readonly ILogger<WebhookEventBridge> _logger;

    public WebhookEventBridge(IDomainEventBus bus, ILogger<WebhookEventBridge> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        => PublishAsync(WebhookEventTypes.OrderPlaced, notification, cancellationToken);

    public Task Handle(OrderRouted notification, CancellationToken cancellationToken)
        => PublishAsync(WebhookEventTypes.OrderRouted, notification, cancellationToken);

    public Task Handle(OrderUnroutable notification, CancellationToken cancellationToken)
        => PublishAsync(WebhookEventTypes.OrderUnroutable, notification, cancellationToken);

    private async Task PublishAsync(string eventType, object data, CancellationToken cancellationToken)
    {
        try
        {
            await _bus.PublishAsync(eventType, data, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue webhook deliveries for {EventType}.", eventType);
        }
    }
}
