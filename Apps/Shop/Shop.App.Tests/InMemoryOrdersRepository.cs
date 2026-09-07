using Acme.Shop.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;
using MagicCSharp.Data.Models;
using MagicCSharp.Infrastructure.Exceptions;

namespace Acme.Shop.App.Tests;

/// <summary>
///     Stands in for <c>OrdersEfRepository</c>. It only has to honour the contract the use cases rely on:
///     assign an id on create, return null for a key that is not there, and apply the filter.
/// </summary>
public class InMemoryOrdersRepository : IOrdersRepository
{
    private readonly Dictionary<long, Order> stored = new Dictionary<long, Order>();
    private readonly Lock gate = new Lock();
    private long nextId = 1;

    public Task<Order> Create(OrderEdit edit)
    {
        lock (gate)
        {
            var order = new Order
            {
                Id = nextId++,
                CustomerId = edit.CustomerId,
                Total = edit.Total,
                Status = edit.Status,
                Created = DateTimeOffset.UtcNow,
                Updated = DateTimeOffset.UtcNow,
            };

            stored[order.Id] = order;
            return Task.FromResult(order);
        }
    }

    public Task<Order?> Get(long key)
    {
        lock (gate)
        {
            return Task.FromResult(stored.GetValueOrDefault(key));
        }
    }

    public Task<Pagination<Order>> Get(PaginationRequest pagination, OrderFilter filter)
    {
        lock (gate)
        {
            var matching = Matching(filter).ToList();

            // pagination.Skip, not Page * PageSize: pages are 1-based, so the first page must skip nothing.
            var page = matching
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToList();

            return Task.FromResult(new Pagination<Order>(pagination, matching.Count, page));
        }
    }

    public Task<IReadOnlyList<Order>> Get(OrderFilter filter)
    {
        lock (gate)
        {
            return Task.FromResult<IReadOnlyList<Order>>(Matching(filter).ToList());
        }
    }

    public Task<int> Count(OrderFilter filter)
    {
        lock (gate)
        {
            return Task.FromResult(Matching(filter).Count());
        }
    }

    public Task<IReadOnlyList<long>> GetKeys(OrderFilter filter)
    {
        lock (gate)
        {
            return Task.FromResult<IReadOnlyList<long>>(Matching(filter).Select(order => order.Id).ToList());
        }
    }

    public Task<IReadOnlyList<Order>> Get(IReadOnlyList<long> keys)
    {
        lock (gate)
        {
            return Task.FromResult<IReadOnlyList<Order>>(
                keys.Where(stored.ContainsKey).Select(key => stored[key]).ToList());
        }
    }

    public Task<Order> Update(long key, OrderEdit edit)
    {
        lock (gate)
        {
            var existing = stored.GetValueOrDefault(key);
            NotFoundException.ThrowIfNull(existing, key);

            var updated = existing with
            {
                CustomerId = edit.CustomerId,
                Total = edit.Total,
                Status = edit.Status,
                Updated = DateTimeOffset.UtcNow,
            };

            stored[key] = updated;
            return Task.FromResult(updated);
        }
    }

    public Task<int> Delete(long key)
    {
        lock (gate)
        {
            return Task.FromResult(stored.Remove(key) ? 1 : 0);
        }
    }

    private IEnumerable<Order> Matching(OrderFilter filter)
    {
        return stored.Values
            .Where(order => filter.Ids == null || filter.Ids.Contains(order.Id))
            .Where(order => filter.CustomerId == null || order.CustomerId == filter.CustomerId)
            .Where(order => filter.Status == null || order.Status == filter.Status)
            .OrderByDescending(order => order.Id);
    }

    // Not reached by these tests. Left to throw rather than half-implemented, so a test that starts using
    // one fails loudly instead of passing against a stub that quietly does nothing.
    public Task<IReadOnlyList<long>> Create(IReadOnlyList<OrderEdit> edits) => throw new NotSupportedException();
    public Task<int> Update(IReadOnlyDictionary<long, OrderEdit> edits) => throw new NotSupportedException();
    public Task<Order> Update(Order entity) => throw new NotSupportedException();
    public Task<int> Update(IReadOnlyList<Order> entities) => throw new NotSupportedException();
    public Task<int> Delete(IReadOnlyList<long> keys) => throw new NotSupportedException();
    public Task<int> Delete(OrderFilter filter) => throw new NotSupportedException();
}
