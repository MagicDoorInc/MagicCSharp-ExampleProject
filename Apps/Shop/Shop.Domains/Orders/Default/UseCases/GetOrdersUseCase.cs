using Acme.Shop.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;
using MagicCSharp.Data.Models;
using MagicCSharp.Data.Repositories;
using MagicCSharp.UseCases;

namespace Acme.Shop.Domains.Orders.UseCases;

public interface IGetOrdersUseCase : IMagicUseCase
{
    Task<Order> ById(long id);

    Task<Pagination<Order>> ForCustomer(long? customerId, OrderStatus? status, PaginationRequest pagination);
}

/// <summary>
///     Reads go through a use case for the same reason writes do: it is the layer the next requirement
///     lands in. When "only the customer who owns it may read an order" arrives, it belongs here, once,
///     rather than in every controller that fetches one.
/// </summary>
public class GetOrdersUseCase(IOrdersRepository orders) : IGetOrdersUseCase
{
    /// <summary>
    ///     Throws rather than returning null. The repository raises the domain's <c>NotFoundException</c>
    ///     and MagicCSharp's error handling turns it into a 404 with a problem+json body — so the caller
    ///     writes no null check and every endpoint reports a missing row the same way.
    /// </summary>
    public Task<Order> ById(long id)
    {
        return orders.GetOrThrow(id);
    }

    public Task<Pagination<Order>> ForCustomer(long? customerId, OrderStatus? status, PaginationRequest pagination)
    {
        return orders.Get(pagination, new OrderFilter { CustomerId = customerId, Status = status });
    }
}
