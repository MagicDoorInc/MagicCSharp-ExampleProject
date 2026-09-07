using Acme.Libraries.Events;
using MagicCSharp.Events.Events;
using Microsoft.Extensions.Logging;

namespace Acme.Notifications.Domains.Delivery.EventHandlers;

/// <summary>
///     The other half of the cross-service contract: Shop publishes <see cref="OrderPlacedEvent" />, this
///     service reacts to it. Neither project references the other — they share only
///     <c>Acme.Libraries.Events</c>, which is what keeps the two deployable independently.
/// </summary>
public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IEventHandler<OrderPlacedEvent>
{
    /// <summary>
    ///     Static, not an instance property. The registration reads it by reflection without constructing
    ///     the handler, so an instance property is silently ignored and every handler ends up at the
    ///     default priority.
    /// </summary>
    public static MagicEventPriority Priority => MagicEventPriority.NotifyUser;

    public Task Handle(OrderPlacedEvent magicEvent)
    {
        // A real one would send mail. The point here is that it runs at all, in a service that shares no
        // code with the publisher beyond the event record.
        logger.LogInformation("Confirming order {OrderId} for customer {CustomerId}, total {Total}",
            magicEvent.OrderId, magicEvent.CustomerId, magicEvent.Total);

        return Task.CompletedTask;
    }
}
