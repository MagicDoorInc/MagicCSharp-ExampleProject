using MagicCSharp.Infrastructure;
using MagicCSharp.Infrastructure.Entities;

namespace Acme.Shop.Domains.Orders.Models.Entities;

/// <summary>An order as the rest of the application sees it.</summary>
public record Order : OrderEdit, IMagicEntity, IIdEntity
{
    public required long Id { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
}

/// <summary>The writable fields. Order derives from this, so an entity is accepted wherever an edit is.</summary>
public record OrderEdit
{
    public required long CustomerId { get; init; }
    public required decimal Total { get; init; }
    public required OrderStatus Status { get; init; }
}

public enum OrderStatus
{
    Pending,
    Paid,
    Cancelled,
}

/// <summary>Query criteria. A null property means "do not narrow on this".</summary>
public class OrderFilter
{
    public List<long>? Ids { get; init; }
    public long? CustomerId { get; init; }
    public OrderStatus? Status { get; init; }
    public ComparableRange<DateTimeOffset>? Created { get; init; }
}
