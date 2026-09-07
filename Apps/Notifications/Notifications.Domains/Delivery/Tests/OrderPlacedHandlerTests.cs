using Acme.Libraries.Events;
using Acme.Notifications.Domains.Delivery.EventHandlers;
using MagicCSharp.Events.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acme.Notifications.Domains.Delivery.Tests;

public class OrderPlacedHandlerTests
{
    [Fact]
    public async Task Handles_an_order_placed_by_the_other_service()
    {
        var handler = new OrderPlacedHandler(NullLogger<OrderPlacedHandler>.Instance);

        await handler.Handle(new OrderPlacedEvent { OrderId = 1, CustomerId = 7, Total = 20m });
    }

    [Fact]
    public void Priority_is_declared_statically()
    {
        // The registration reads Priority by reflection without constructing the handler, so an instance
        // property is ignored and every handler silently lands on the default. This asserts the shape the
        // runtime actually reads.
        var priority = typeof(OrderPlacedHandler)
            .GetProperty(nameof(OrderPlacedHandler.Priority),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        Assert.NotNull(priority);
        Assert.Equal(MagicEventPriority.NotifyUser, priority.GetValue(null));
    }
}
