using MagicCSharp.Infrastructure;
using MagicCSharp.Infrastructure.Entities;

namespace Acme.Shop.Domains.Orders.Models.Entities;

/// <summary>The ApiKey as the rest of the application sees it.</summary>
public record ApiKey : ApiKeyEdit, IMagicEntity, IKeyEntity
{
    public required string Key { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
}

/// <summary>The writable fields. ApiKey derives from this, so an entity is accepted wherever an edit is.</summary>
public record ApiKeyEdit
{
    // TODO: add the writable fields
}

/// <summary>Query criteria. Every property is optional; a null one means "do not narrow on this".</summary>
public class ApiKeyFilter
{
    public List<string>? Keys { get; init; }
    public ComparableRange<DateTimeOffset>? Created { get; init; }
}
