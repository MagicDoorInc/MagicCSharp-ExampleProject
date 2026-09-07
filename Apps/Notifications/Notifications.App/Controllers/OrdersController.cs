using Acme.Libraries.Events;
using MagicCSharp.Events.Events;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Notifications.App.Controllers;

/// <summary>
///     Stands in for the Kafka or SQS listener a real deployment would have: it takes the event off the
///     wire and hands it to the dispatcher, which is the only part the handler cares about. Swapping the
///     transport later does not touch <c>OrderPlacedHandler</c>.
/// </summary>
[ApiController]
[Route("internal/events")]
public class OrdersController(IEventDispatcher events, IEventTypeHolder handlers) : ControllerBase
{
    [HttpPost("order-placed")]
    public IActionResult OrderPlaced([FromBody] OrderPlacedEvent placed)
    {
        events.Dispatch(placed);
        return Accepted();
    }

    /// <summary>Which handlers the discovery pass actually found. Useful when one silently does not run.</summary>
    [HttpGet("handlers")]
    public IActionResult Handlers()
    {
        return Ok(handlers.GetHandlerTypes(typeof(OrderPlacedEvent)).Select(handler => handler.Name));
    }
}
