using Acme.Shop.Domains.Orders.Models.Entities;
using Acme.Shop.Domains.Orders.UseCases;
using MagicCSharp.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Shop.Domains.Orders.App;

/// <summary>
///     The thin end of the slice: HTTP in, use case, HTTP out. It holds no logic of its own and touches no
///     repository — everything worth testing lives behind <see cref="IPlaceOrderUseCase" /> and
///     <see cref="IGetOrdersUseCase" />, where a test reaches it without a web host or a database.
///     <para>
///         This project is the Orders domain's own HTTP surface, not the service's. The host references it
///         and serves these routes with no other wiring, which is what keeps <c>Shop.App</c> a shell rather
///         than a folder collecting every domain's controllers.
///     </para>
///     <para>
///         Neither use case is registered anywhere by hand. MagicCSharp finds every implementation of
///         <c>IMagicUseCase</c> at startup and registers it under its own interface.
///     </para>
/// </summary>
[ApiController]
[Route("orders")]
public class OrdersController(IPlaceOrderUseCase placeOrder, IGetOrdersUseCase getOrders) : ControllerBase
{
    public record PlaceOrderBody(long CustomerId, decimal Total);

    [HttpPost]
    public async Task<ActionResult<Order>> Place([FromBody] PlaceOrderBody body)
    {
        var order = await placeOrder.Execute(new PlaceOrderRequest(body.CustomerId, body.Total));

        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    /// <summary>No null check: a missing order throws, and error handling makes that a 404.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Order>> Get(long id)
    {
        return await getOrders.ById(id);
    }

    /// <summary>Pages are 1-based, matching <see cref="PaginationRequest" />.</summary>
    [HttpGet]
    public async Task<ActionResult<Pagination<Order>>> List(
        [FromQuery] long? customerId,
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        return await getOrders.ForCustomer(customerId, status, new PaginationRequest(pageSize, page));
    }
}
