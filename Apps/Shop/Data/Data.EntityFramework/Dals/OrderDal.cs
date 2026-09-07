using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Acme.Shop.Domains.Orders.Models.Entities;
using MagicCSharp.Data.EntityFramework.Dals;

namespace Acme.Shop.Data.EntityFramework.Dals;

[Table("orders")]
public class OrderDal : BaseIdDal<Order, OrderEdit>
{
    [Required]
    [Column("customer_id")]
    public long CustomerId { get; set; }

    [Required]
    [Column("total")]
    public decimal Total { get; set; }

    [Required]
    [Column("status")]
    public OrderStatus Status { get; set; }

    /// <summary>Row to entity.</summary>
    public override Order ToEntity()
    {
        return new Order
        {
            Id = Id,
            CustomerId = CustomerId,
            Total = Total,
            Status = Status,
            Created = Created,
            Updated = Updated,
        };
    }

    /// <summary>
    ///     Edit onto row. Every writable field is assigned unconditionally — the repository decides whether
    ///     anything actually changed by asking the change tracker afterwards, and only then stamps Updated.
    /// </summary>
    public override void Apply(OrderEdit edit)
    {
        CustomerId = edit.CustomerId;
        Total = edit.Total;
        Status = edit.Status;
    }

    public static OrderDal From(OrderEdit edit, long id)
    {
        var dal = new OrderDal { Id = id };
        dal.Apply(edit);
        return dal;
    }
}
