using MagicCSharp.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;

namespace Acme.Shop.Data.Repositories;

public interface IOrdersRepository :
    IRepository<Order, long, OrderEdit, OrderFilter>,
    IPaginatedRepository<Order, OrderFilter>
{
}
