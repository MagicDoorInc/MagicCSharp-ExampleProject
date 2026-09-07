using Acme.Libraries.Events;
using Acme.Shop.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;
using MagicCSharp.Events.Events;
using MagicCSharp.UseCases;

namespace Acme.Shop.Domains.Orders.UseCases;

public record PlaceOrderRequest(long CustomerId, decimal Total);

public interface IPlaceOrderUseCase : IMagicUseCase
{
    Task<Order> Execute(PlaceOrderRequest request);
}

/// <summary>
///     Business logic, and nothing else. No HTTP, no Entity Framework, no configuration — which is what
///     makes it constructible in a test with two fakes and no host.
/// </summary>
public class PlaceOrderUseCase(IOrdersRepository orders, IEventDispatcher events) : IPlaceOrderUseCase
{
    public async Task<Order> Execute(PlaceOrderRequest request)
    {
        var order = await orders.Create(new OrderEdit
        {
            CustomerId = request.CustomerId,
            Total = request.Total,
            Status = OrderStatus.Pending,
        });

        // Dispatched after the write, so a handler that reads the order finds it. Fire-and-forget, the same
        // whether this is in-process or Kafka.
        events.Dispatch(new OrderPlacedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Total = order.Total,
        });

        return order;
    }
}
