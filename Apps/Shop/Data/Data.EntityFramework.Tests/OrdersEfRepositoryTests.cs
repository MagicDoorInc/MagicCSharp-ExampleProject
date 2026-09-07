using Acme.Shop.Data.EntityFramework.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;
using MagicCSharp.Data.Models;
using MagicCSharp.Infrastructure;
using MagicCSharp.Testing.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acme.Shop.Data.EntityFramework.Tests;

/// <summary>
///     The repository against a real PostgreSQL in a container.
///     <para>
///         The use-case tests elsewhere in this repository replace <c>IOrdersRepository</c> with a fake,
///         which is right for testing business logic and proves nothing about the repository itself. A
///         repository is mostly translation — a filter into SQL, a row into an entity — and only a database
///         can say whether the translation is correct. These are the tests for that half.
///     </para>
///     <para>
///         Needs Docker. <c>dotnet test --filter "Category!=Database"</c> skips them.
///     </para>
/// </summary>
[Trait("Category", "Database")]
public class OrdersEfRepositoryTests : TestRepositoryBase<MagicShopContext>
{
    /// <summary>
    ///     Runs the committed migration rather than building the schema from the model. That makes these
    ///     tests catch a migration that has drifted from the entities — the failure that otherwise waits
    ///     until a deploy.
    /// </summary>
    protected override Task InitializeDatabase(MagicShopContext context)
    {
        return context.Database.MigrateAsync();
    }

    private OrdersEfRepository Orders =>
        new OrdersEfRepository(DbContextFactory, KeyGen, Clock, NullLoggerFactory.Instance);

    private static OrderEdit AnOrder(long customerId = 1, decimal total = 10m, OrderStatus status = OrderStatus.Pending)
    {
        return new OrderEdit { CustomerId = customerId, Total = total, Status = status };
    }

    [Fact]
    public async Task An_order_round_trips_through_the_database()
    {
        var created = await Orders.Create(AnOrder(customerId: 7, total: 42.50m));

        var read = await Orders.Get(created.Id);

        Assert.NotNull(read);
        Assert.Equal(7, read.CustomerId);
        Assert.Equal(42.50m, read.Total);
        Assert.Equal(OrderStatus.Pending, read.Status);
    }

    [Fact]
    public async Task The_id_is_a_snowflake_assigned_before_the_insert()
    {
        var created = await Orders.Create(AnOrder());

        Assert.NotEqual(0, created.Id);
    }

    [Fact]
    public async Task A_decimal_keeps_its_scale()
    {
        // numeric, not float. A total of 42.50 that comes back as 42.499999 is a rounding bug nobody finds
        // until it reaches an invoice.
        var created = await Orders.Create(AnOrder(total: 1234.56m));

        Assert.Equal(1234.56m, (await Orders.Get(created.Id))!.Total);
    }

    [Fact]
    public async Task The_status_column_holds_the_name_not_the_number()
    {
        var created = await Orders.Create(AnOrder(status: OrderStatus.Paid));

        await using var context = DbContextFactory.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT status FROM orders WHERE id = {created.Id}";

        // Inserting a member above Paid would renumber it; the name does not move.
        Assert.Equal("Paid", (await command.ExecuteScalarAsync())?.ToString());
    }

    [Fact]
    public async Task Filtering_by_customer_narrows_the_result()
    {
        await Orders.Create(AnOrder(customerId: 1));
        await Orders.Create(AnOrder(customerId: 2));

        var mine = await Orders.Get(new OrderFilter { CustomerId = 1 });

        Assert.Equal(1, Assert.Single(mine).CustomerId);
    }

    [Fact]
    public async Task Filtering_by_status_narrows_the_result()
    {
        await Orders.Create(AnOrder(status: OrderStatus.Pending));
        await Orders.Create(AnOrder(status: OrderStatus.Cancelled));

        Assert.Single(await Orders.Get(new OrderFilter { Status = OrderStatus.Cancelled }));
    }

    [Fact]
    public async Task Filtering_by_a_list_of_ids_returns_only_those()
    {
        var wanted = await Orders.Create(AnOrder());
        await Orders.Create(AnOrder());

        var found = await Orders.Get(new OrderFilter { Ids = [wanted.Id] });

        Assert.Equal(wanted.Id, Assert.Single(found).Id);
    }

    [Fact]
    public async Task Filtering_by_a_created_range_uses_the_clock()
    {
        Clock.SetTime(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await Orders.Create(AnOrder(customerId: 111));

        Clock.Advance(TimeSpan.FromDays(30));
        var recent = await Orders.Create(AnOrder(customerId: 222));

        var found = await Orders.Get(new OrderFilter
        {
            Created = new ComparableRange<DateTimeOffset> { Start = Clock.Now().AddDays(-1) },
        });

        Assert.Equal(recent.Id, Assert.Single(found).Id);
    }

    [Fact]
    public async Task Filters_combine_rather_than_replace_each_other()
    {
        await Orders.Create(AnOrder(customerId: 1, status: OrderStatus.Pending));
        await Orders.Create(AnOrder(customerId: 1, status: OrderStatus.Paid));
        await Orders.Create(AnOrder(customerId: 2, status: OrderStatus.Pending));

        var found = await Orders.Get(new OrderFilter { CustomerId = 1, Status = OrderStatus.Pending });

        Assert.Single(found);
    }

    [Fact]
    public async Task Paging_is_one_based_and_reports_the_whole_total()
    {
        for (var i = 0; i < 5; i++)
        {
            await Orders.Create(AnOrder());
        }

        var first = await Orders.Get(new PaginationRequest(2, 1), new OrderFilter());
        var second = await Orders.Get(new PaginationRequest(2, 2), new OrderFilter());

        Assert.Equal(5, first.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.Empty(first.Items.Select(o => o.Id).Intersect(second.Items.Select(o => o.Id)));
    }

    [Fact]
    public async Task Timestamps_are_stored_as_utc()
    {
        Clock.SetTime(new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.FromHours(5)));

        var created = await Orders.Create(AnOrder());
        var read = await Orders.Get(created.Id);

        Assert.Equal(TimeSpan.Zero, read!.Created.Offset);
    }

    [Fact]
    public async Task Updating_an_order_that_is_not_there_throws()
    {
        await Assert.ThrowsAsync<MagicCSharp.Infrastructure.Exceptions.NotFoundIdException>(
            () => Orders.Update(404L, AnOrder()));
    }
}
