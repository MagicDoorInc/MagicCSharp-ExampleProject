using Acme.Shop.Data.EntityFramework.Dals;
using Acme.Shop.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;
using MagicCSharp.Data.EntityFramework.Repositories;
using MagicCSharp.Data.Utils;
using MagicCSharp.Infrastructure;
using MagicCSharp.Infrastructure.KeyGen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acme.Shop.Data.EntityFramework.Repositories;

public class OrdersEfRepository(
    IDbContextFactory<MagicShopContext> contextFactory,
    IKeyGenService keyGen,
    IClock clock,
    ILoggerFactory loggerFactory)
    : BaseIdPaginatedRepository<MagicShopContext, OrderDal, Order, OrderFilter, OrderEdit>(
        contextFactory, clock, loggerFactory), IOrdersRepository
{
    protected override OrderDal CreateDal(OrderEdit edit)
    {
        // The id is assigned here, before insert, so the caller knows it without a round trip.
        return OrderDal.From(edit, keyGen.GetId());
    }

    protected override IQueryable<OrderDal> ApplyFilter(IQueryable<OrderDal> query, OrderFilter filter)
    {
        query = query.ApplyListFilter(filter.Ids, x => (long?)x.Id);
        query = query.ApplyNullableValueFilter(filter.CustomerId, x => (long?)x.CustomerId);
        query = query.ApplyNullableValueFilter(filter.Status, x => (OrderStatus?)x.Status);
        query = query.ApplyComparableRangeFilter(filter.Created, x => (DateTimeOffset?)x.Created);

        return query;
    }
}
