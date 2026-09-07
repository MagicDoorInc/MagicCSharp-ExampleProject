using MagicCSharp.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;

namespace Acme.Shop.Data.Repositories;

public interface IApiKeysRepository :
    IRepository<ApiKey, string, ApiKeyEdit, ApiKeyFilter>
{
}
