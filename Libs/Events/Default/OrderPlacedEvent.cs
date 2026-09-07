using MagicCSharp.Events.Events;

namespace Acme.Libraries.Events;

/// <summary>
///     Raised by Shop when an order is placed, handled by Notifications.
///     <para>
///         It carries ids and primitives, never an entity. An event is deserialized by code built from a
///         different commit than the one that published it, so a property typed as <c>Order</c> would tie
///         this contract to that class's shape — add a field on one side and the other reads nulls. The
///         handler looks up whatever else it needs.
///     </para>
/// </summary>
public record OrderPlacedEvent : MagicEvent
{
    public required long OrderId { get; init; }
    public required long CustomerId { get; init; }
    public required decimal Total { get; init; }
}
