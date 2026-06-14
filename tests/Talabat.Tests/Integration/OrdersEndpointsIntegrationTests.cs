using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Talabat.APIs.Dtos;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Models;
using Talabat.Repository.Data;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Integration;

public class OrdersEndpointsIntegrationTests : IClassFixture<TalabatApiFactory>
{
    private readonly TalabatApiFactory factory;

    public OrdersEndpointsIntegrationTests(TalabatApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetOrdersForUser_ReturnsSeededOrders()
    {
        await factory.ResetStoreAsync(BuildSeedOrders());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderToReturnDto>>();
        var seededOrders = orders ?? throw new InvalidOperationException("Expected orders payload");
        seededOrders.Should().HaveCount(3);
        seededOrders[0].Id.Should().Be(3);
    }

    [Fact]
    public async Task GetOrderById_ReturnsSeededOrder()
    {
        await factory.ResetStoreAsync(BuildSeedOrders());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/orders/2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await response.Content.ReadFromJsonAsync<OrderToReturnDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(2);
        order.BuyerEmail.Should().Be("buyer@test.com");
    }

    [Fact]
    public async Task GetOrderById_ReturnsNotFoundForMissingOrder()
    {
        await factory.ResetStoreAsync(BuildSeedOrders());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/orders/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDeliveryMethods_ReturnsSeededMethods()
    {
        await factory.ResetStoreAsync(BuildSeedOrders());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/orders/deliverymethods");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deliveryMethods = await response.Content.ReadFromJsonAsync<List<DeliveryMethod>>();
        deliveryMethods.Should().NotBeNull();
        deliveryMethods!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderSummary_ReturnsComputedSummary()
    {
        await factory.ResetStoreAsync(BuildSeedOrders());
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/orders/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<OrderSummary>();
        summary.Should().NotBeNull();
        summary!.TotalOrders.Should().Be(3);
        summary.PendingOrders.Should().Be(1);
        summary.PaymentReceivedOrders.Should().Be(1);
        summary.PaymentFailedOrders.Should().Be(1);
        summary.TotalSpent.Should().Be(105m);
        summary.LatestOrderDate.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrder_PersistsOrderAndReturnsOk()
    {
        await factory.ResetStoreAsync();
        factory.SeedBasket(TestDataFactory.CreateBasket(
            "basket-1",
            new[]
            {
                TestDataFactory.CreateBasketItem(1, 10m, 2),
                TestDataFactory.CreateBasketItem(2, 20m, 1)
            },
            paymentIntentId: "pi_test",
            deliveryMethodId: 1));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/orders", new OrderDto
        {
            BasketId = "basket-1",
            DeliveryMethodId = 1,
            ShippingAddress = new AddressDto
            {
                FirstName = "Ahmed",
                LastName = "Nasser",
                Street = "Street 1",
                City = "Cairo",
                Country = "Egypt"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<StoreContext>();
        var orders = await store.Orders.Include(o => o.Items).Include(o => o.DeliveryMethod).ToListAsync();

        orders.Should().HaveCount(1);
        orders[0].BuyerEmail.Should().Be("buyer@test.com");
        orders[0].SubTotal.Should().Be(40m);
        orders[0].GetTotal().Should().Be(45m);
    }

    private static IReadOnlyList<Order> BuildSeedOrders()
    {
        return new List<Order>
        {
            TestDataFactory.CreateOrder(
                1,
                "buyer@test.com",
                20m,
                OrderStatus.pending,
                new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero)),
            TestDataFactory.CreateOrder(
                2,
                "buyer@test.com",
                30m,
                OrderStatus.PaymentReceieved,
                new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero)),
            TestDataFactory.CreateOrder(
                3,
                "buyer@test.com",
                40m,
                OrderStatus.PaymentFailed,
                new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero))
        };
    }
}
