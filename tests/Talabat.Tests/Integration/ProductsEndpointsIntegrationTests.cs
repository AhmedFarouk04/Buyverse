using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Talabat.APIs.Dtos;
using Talabat.APIs.Helper;
using Talabat.Core.Entities;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Integration;

public class ProductsEndpointsIntegrationTests : IClassFixture<TalabatApiFactory>
{
    private readonly TalabatApiFactory factory;

    public ProductsEndpointsIntegrationTests(TalabatApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetProducts_ReturnsSeededCatalog()
    {
        await factory.ResetStoreAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/products?pageIndex=1&pageSize=4");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<PaginationResponse<ProductToReturnDto>>();
        payload.Should().NotBeNull();
        payload!.Count.Should().Be(4);
        payload.Data.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetProduct_ReturnsSingleProduct()
    {
        await factory.ResetStoreAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/products/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await response.Content.ReadFromJsonAsync<ProductToReturnDto>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(1);
        product.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetProduct_ReturnsNotFoundForMissingProduct()
    {
        await factory.ResetStoreAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/products/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllBrands_ReturnsSeededBrands()
    {
        await factory.ResetStoreAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/products/brands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var brands = await response.Content.ReadFromJsonAsync<List<ProductBrand>>();
        brands.Should().NotBeNull();
        brands!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllTypes_ReturnsSeededTypes()
    {
        await factory.ResetStoreAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/products/Types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var types = await response.Content.ReadFromJsonAsync<List<ProductType>>();
        types.Should().NotBeNull();
        types!.Should().HaveCount(2);
    }

    [Fact]
    public async Task BuggyNotFound_Returns404Response()
    {
        await factory.ResetStoreAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/buggy/notfound");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BuggyServerError_Returns500Json()
    {
        await factory.ResetStoreAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/buggy/servererror");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"statusCode\":500");
    }

    private sealed class PaginationResponse<T>
    {
        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int Count { get; set; }

        public List<T> Data { get; set; } = new();
    }
}
