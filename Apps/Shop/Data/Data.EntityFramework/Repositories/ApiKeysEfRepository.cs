using MagicCSharp.Data.EntityFramework.Repositories;
using MagicCSharp.Data.Utils;
using MagicCSharp.Infrastructure;
using MagicCSharp.Infrastructure.KeyGen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Acme.Shop.Data.EntityFramework.Dals;
using Acme.Shop.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;

namespace Acme.Shop.Data.EntityFramework.Repositories;

public class ApiKeysEfRepository(
    IDbContextFactory<MagicShopContext> contextFactory,
    IKeyGenService keyGen,
    IClock clock,
    ILoggerFactory loggerFactory)
    : BaseKeyRepository<MagicShopContext, ApiKeyDal, ApiKey, ApiKeyFilter, ApiKeyEdit>(
        contextFactory, clock, loggerFactory), IApiKeysRepository
{
    protected override ApiKeyDal CreateDal(ApiKeyEdit edit)
    {
        return ApiKeyDal.From(edit, keyGen.GetKey());
    }

    protected override IQueryable<ApiKeyDal> ApplyFilter(IQueryable<ApiKeyDal> query, ApiKeyFilter filter)
    {
        query = query.ApplyListFilter(filter.Keys, x => x.Key);
        query = query.ApplyComparableRangeFilter(filter.Created, x => (DateTimeOffset?)x.Created);

        // TODO: narrow on the rest of the filter's properties

        return query;
    }
}
