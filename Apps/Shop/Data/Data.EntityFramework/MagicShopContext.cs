using Acme.Shop.Data.EntityFramework.Dals;
using MagicCSharp.Data.EntityFramework;
using MagicCSharp.Data.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Acme.Shop.Data.EntityFramework;

/// <summary>
///     Deriving from MagicDbContext gets two conventions: enums stored by name, so inserting an enum member
///     does not change what existing rows mean, and timestamps normalized to UTC before they are written.
/// </summary>
public class MagicShopContext(DbContextOptions options) : MagicDbContext(options)
{
    // AddEntity adds a DbSet here for each entity it scaffolds.

    public DbSet<OrderDal> Orders { get; set; } = null!;

    public DbSet<ApiKeyDal> ApiKeys { get; set; } = null!;
}

/// <summary>
///     Used by <c>dotnet ef</c>, which builds the context without the application's DI container.
///     Reads DB_HOST / DB_PORT / DB_NAME / DB_USER / DB_PASSWORD, falling back to the values below.
/// </summary>
public class MagicShopContextFactory() : MagicDbContextFactory<MagicShopContext>("shop");
