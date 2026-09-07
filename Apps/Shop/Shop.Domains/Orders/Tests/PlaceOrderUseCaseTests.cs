using Acme.Libraries.Events;
using Acme.Shop.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;
using Acme.Shop.Domains.Orders.UseCases;
using MagicCSharp.Data.Models;
using MagicCSharp.Events.Events;
using MagicCSharp.Testing;

namespace Acme.Shop.Domains.Orders.Tests;

/// <summary>
///     What testing a use case looks like: two fakes, no host, no database, no configuration. This is the
///     whole reason business logic lives in a use case rather than a controller.
/// </summary>
public class PlaceOrderUseCaseTests
{
    private readonly FakeClock clock = new FakeClock();
    private readonly FakeOrdersRepository orders = new FakeOrdersRepository();
    private readonly RecordingEventDispatcher events = new RecordingEventDispatcher();

    [Fact]
    public async Task Places_the_order_as_pending()
    {
        var useCase = new PlaceOrderUseCase(orders, events);

        var order = await useCase.Execute(new PlaceOrderRequest(CustomerId: 7, Total: 42.50m));

        Assert.Equal(7, order.CustomerId);
        Assert.Equal(42.50m, order.Total);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public async Task Announces_the_order_so_other_services_can_react()
    {
        var useCase = new PlaceOrderUseCase(orders, events);

        var order = await useCase.Execute(new PlaceOrderRequest(CustomerId: 7, Total: 42.50m));

        var published = Assert.Single(events.Dispatched.OfType<OrderPlacedEvent>());
        Assert.Equal(order.Id, published.OrderId);
        Assert.Equal(7, published.CustomerId);
    }

    [Fact]
    public async Task The_event_carries_the_id_of_an_order_that_already_exists()
    {
        // Dispatch happens after the write, so a handler that immediately looks the order up finds it.
        var useCase = new PlaceOrderUseCase(orders, events);

        var order = await useCase.Execute(new PlaceOrderRequest(CustomerId: 7, Total: 1m));
        var published = events.Dispatched.OfType<OrderPlacedEvent>().Single();

        Assert.NotNull(await orders.Get(published.OrderId));
        Assert.Equal(order.Id, published.OrderId);
    }
}

/// <summary>An in-memory stand-in — the use case only knows the interface, so this is all it takes.</summary>
public class FakeOrdersRepository : IOrdersRepository
{
    private readonly Dictionary<long, Order> stored = new Dictionary<long, Order>();
    private long nextId = 1;

    public Task<Order> Create(OrderEdit edit)
    {
        var order = new Order
        {
            Id = nextId++,
            CustomerId = edit.CustomerId,
            Total = edit.Total,
            Status = edit.Status,
            Created = DateTimeOffset.UnixEpoch,
            Updated = DateTimeOffset.UnixEpoch,
        };

        stored[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<Order?> Get(long key) => Task.FromResult(stored.GetValueOrDefault(key));
    public Task<IReadOnlyList<Order>> Get(OrderFilter filter) => Task.FromResult<IReadOnlyList<Order>>(stored.Values.ToList());
    public Task<IReadOnlyList<Order>> Get(IReadOnlyList<long> keys) => Task.FromResult<IReadOnlyList<Order>>(keys.Where(stored.ContainsKey).Select(k => stored[k]).ToList());
    public Task<int> Count(OrderFilter filter) => Task.FromResult(stored.Count);
    public Task<IReadOnlyList<long>> GetKeys(OrderFilter filter) => Task.FromResult<IReadOnlyList<long>>(stored.Keys.ToList());
    public Task<IReadOnlyList<long>> Create(IReadOnlyList<OrderEdit> edits) => throw new NotSupportedException();
    public Task<Order> Update(long key, OrderEdit edit) => throw new NotSupportedException();
    public Task<int> Update(IReadOnlyDictionary<long, OrderEdit> edits) => throw new NotSupportedException();
    public Task<Order> Update(Order entity) => throw new NotSupportedException();
    public Task<int> Update(IReadOnlyList<Order> entities) => throw new NotSupportedException();
    public Task<int> Delete(long key) => Task.FromResult(stored.Remove(key) ? 1 : 0);
    public Task<int> Delete(IReadOnlyList<long> keys) => throw new NotSupportedException();
    public Task<int> Delete(OrderFilter filter) => throw new NotSupportedException();
    public Task<Pagination<Order>> Get(PaginationRequest pagination, OrderFilter filter) =>
        Task.FromResult(new Pagination<Order>(pagination, stored.Count, stored.Values.ToList()));
}

/// <summary>Records what was dispatched, so a test can assert on it.</summary>
public class RecordingEventDispatcher : IEventDispatcher
{
    public List<MagicEvent> Dispatched { get; } = [];

    public void Dispatch(MagicEvent? magicEvent)
    {
        if (magicEvent != null)
        {
            Dispatched.Add(magicEvent);
        }
    }
}
