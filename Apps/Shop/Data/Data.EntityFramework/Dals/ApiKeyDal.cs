using System.ComponentModel.DataAnnotations.Schema;
using MagicCSharp.Data.EntityFramework.Dals;
using Acme.Shop.Domains.Orders.Models.Entities;

namespace Acme.Shop.Data.EntityFramework.Dals;

[Table("api_keys")]
public class ApiKeyDal : BaseKeyDal<ApiKey, ApiKeyEdit>
{
    // TODO: add the columns, each with [Column("snake_case_name")] and [Required] where it is not nullable

    /// <summary>Row to entity. Read every column the entity exposes.</summary>
    public override ApiKey ToEntity()
    {
        return new ApiKey
        {
            Key = Key,
            // TODO: map the columns
            Created = Created,
            Updated = Updated,
        };
    }

    /// <summary>
    ///     Edit onto row. Assign every writable field unconditionally — the repository decides whether
    ///     anything changed by asking the change tracker afterwards.
    /// </summary>
    public override void Apply(ApiKeyEdit edit)
    {
        // TODO: map the columns
    }

    /// <summary>Build a new row. The key is assigned by the caller, before insert.</summary>
    public static ApiKeyDal From(ApiKeyEdit edit, string key)
    {
        var dal = new ApiKeyDal
        {
            Key = key,
        };
        dal.Apply(edit);
        return dal;
    }
}
