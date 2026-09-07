using Acme.Shop.Data.EntityFramework.Repositories;
using Acme.Shop.Data.Repositories;
using MagicCSharp.Data.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Shop.Data.EntityFramework;

public static class ShopRepositoriesModule
{
    /// <summary>
    ///     Registers the context factory and every repository.
    ///     <para>
    ///         Repositories are registered explicitly rather than discovered, so the list of things that talk
    ///         to the database is readable in one place. AddEntity appends to it.
    ///     </para>
    /// </summary>
    public static IServiceCollection AddShopRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresDbContextFactory<MagicShopContext>(configuration);

                services.AddScoped<IOrdersRepository, OrdersEfRepository>();

                services.AddScoped<IApiKeysRepository, ApiKeysEfRepository>();

        return services;
    }
}
