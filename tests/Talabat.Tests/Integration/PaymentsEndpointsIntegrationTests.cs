using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Talabat.APIs.Dtos;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Integration;

public class PaymentsEndpointsIntegrationTests : IClassFixture<TalabatApiFactory>
{
    private readonly TalabatApiFactory factory;

    public PaymentsEndpointsIntegrationTests(TalabatApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ReturnsBasketWhenBasketExists()
    {
        await factory.ResetStoreAsync();
        factory.SeedBasket(TestDataFactory.CreateBasket(
            "basket-1",
            new[]
            {
                TestDataFactory.CreateBasketItem(1, 10m, 1)
            },
            paymentIntentId: "pi_test"));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/api/payments/basket-1", new StringContent(string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var basket = await response.Content.ReadFromJsonAsync<CustomerBsketDto>();
        basket.Should().NotBeNull();
        basket!.Id.Should().Be("basket-1");
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ReturnsBadRequestWhenBasketMissing()
    {
        await factory.ResetStoreAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/api/payments/missing-basket", new StringContent(string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
