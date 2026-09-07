using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acme.Shop.App;
using Acme.Shop.Data.Repositories;
using Acme.Shop.Domains.Orders.Models.Entities;
using MagicCSharp.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Acme.Shop.App.Tests;

/// <summary>
///     The service end to end, with the database swapped out.
///     <para>
///         Everything above the repository is the real thing: routing, model binding, the use cases,
///         dependency injection, the request-id middleware and the problem-details error handling. Only
///         <see cref="IOrdersRepository" /> is replaced, so these run in a second with no Postgres and no
///         Docker — which is what makes them worth running on every commit.
///     </para>
/// </summary>
public class OrdersEndpointTests : IClassFixture<ShopFactory>
{
    /// <summary>
    ///     What any client of this API needs. The service sends enums as names — <c>"Pending"</c>, not
    ///     <c>0</c> — so a reader using stock System.Text.Json throws on the first order it parses. The
    ///     converter is the whole fix, and it is the same one the service configures on its side.
    /// </summary>
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient client;

    public OrdersEndpointTests(ShopFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Placing_an_order_returns_it_with_an_id()
    {
        var response = await client.PostAsJsonAsync("/orders", new { customerId = 7, total = 42.50m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<Order>(Json);

        Assert.NotNull(order);
        Assert.NotEqual(0, order.Id);
        Assert.Equal(7, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public async Task A_placed_order_can_be_read_back()
    {
        var placed = await (await client.PostAsJsonAsync("/orders", new { customerId = 9, total = 5m }))
            .Content.ReadFromJsonAsync<Order>(Json);

        var fetched = await client.GetFromJsonAsync<Order>($"/orders/{placed!.Id}", Json);

        Assert.Equal(placed.Id, fetched!.Id);
    }

    [Fact]
    public async Task An_order_that_does_not_exist_is_a_404_not_a_500()
    {
        // The use case does not check for null. The repository throws the domain's NotFoundException and
        // MagicCSharp's error handling turns it into this — which is the whole point of that arrangement.
        var response = await client.GetAsync("/orders/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_missing_order_is_reported_as_problem_json()
    {
        var response = await client.GetAsync("/orders/999999");

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        Assert.Equal("NotFound", problem.GetProperty("title").GetString());
        Assert.Equal("/orders/999999", problem.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task An_error_body_carries_the_same_request_id_as_the_header()
    {
        // These were once different values — the body had Kestrel's connection counter and the header had
        // the real id, so an id quoted from a bug report matched nothing in the logs.
        var response = await client.GetAsync("/orders/999999");

        var problem = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        Assert.Equal(
            response.Headers.GetValues("X-Request-ID").Single(),
            problem.GetProperty("requestId").GetString());
    }

    [Fact]
    public async Task A_caller_supplied_request_id_is_kept()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/orders/999999");
        request.Headers.Add("X-Request-ID", "trace-me-across-services");

        var response = await client.SendAsync(request);

        Assert.Equal("trace-me-across-services", response.Headers.GetValues("X-Request-ID").Single());
    }

    [Fact]
    public async Task Listing_orders_comes_back_paginated()
    {
        await client.PostAsJsonAsync("/orders", new { customerId = 11, total = 1m });

        var page = await client.GetFromJsonAsync<Pagination<Order>>("/orders?page=1&pageSize=10", Json);

        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 1);
        Assert.NotEmpty(page.Items);
    }

    [Fact]
    public async Task The_first_page_starts_at_page_one_not_page_zero()
    {
        // Pages are 1-based and PaginationRequest.Skip is PageSize * (Page - 1). Treating them as 0-based
        // silently drops the first page of every list.
        var customer = Random.Shared.NextInt64(1000, 999999);

        foreach (var total in new[] { 1m, 2m, 3m })
        {
            await client.PostAsJsonAsync("/orders", new { customerId = customer, total });
        }

        var first = await client.GetFromJsonAsync<Pagination<Order>>($"/orders?customerId={customer}&page=1&pageSize=2", Json);
        var second = await client.GetFromJsonAsync<Pagination<Order>>($"/orders?customerId={customer}&page=2&pageSize=2", Json);

        Assert.Equal(3, first!.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.Single(second!.Items);
        Assert.Empty(first.Items.Select(order => order.Id).Intersect(second.Items.Select(order => order.Id)));
    }

    [Fact]
    public async Task An_enum_goes_out_as_its_name_not_a_number()
    {
        // The database stores OrderStatus by name, and so does the API. A client that hard-coded 0 would
        // break the day someone inserted a member above Pending, and nothing would report it.
        var response = await client.PostAsJsonAsync("/orders", new { customerId = 3, total = 1m });

        Assert.Contains("\"status\":\"Pending\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_enum_can_be_filtered_by_name()
    {
        var customer = Random.Shared.NextInt64(1000, 999999);
        await client.PostAsJsonAsync("/orders", new { customerId = customer, total = 1m });

        var pending = await client.GetFromJsonAsync<Pagination<Order>>($"/orders?customerId={customer}&status=Pending", Json);
        var cancelled = await client.GetFromJsonAsync<Pagination<Order>>($"/orders?customerId={customer}&status=Cancelled", Json);

        Assert.Equal(1, pending!.TotalCount);
        Assert.Equal(0, cancelled!.TotalCount);
    }

    [Fact]
    public async Task A_filter_narrows_the_list()
    {
        var customer = Random.Shared.NextInt64(1000, 999999);
        await client.PostAsJsonAsync("/orders", new { customerId = customer, total = 1m });

        var mine = await client.GetFromJsonAsync<Pagination<Order>>($"/orders?customerId={customer}", Json);

        Assert.Single(mine!.Items);
        Assert.All(mine.Items, order => Assert.Equal(customer, order.CustomerId));
    }
}

/// <summary>
///     Boots the real service, then replaces the one thing that needs a database.
/// </summary>
public class ShopFactory : WebApplicationFactory<ShopProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // The data layer opens a connection when it registers, so a wrong password fails the deploy rather
        // than the first request. There is no database here and every repository is replaced below, so turn
        // that check off for these tests only.
        builder.UseSetting("DB_VERIFY_CONNECTION", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOrdersRepository>();
            services.AddSingleton<IOrdersRepository, InMemoryOrdersRepository>();
        });
    }
}
