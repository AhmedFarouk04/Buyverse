using FluentAssertions;
using Talabat.Core.Entities;
using Talabat.Core.Specifications;
using Talabat.Repository;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Specifications;

public class ProductSpecificationTests
{
    [Fact]
    public async Task FilterBySearch_ShouldReturnMatchingProducts()
    {
        await using var context = TestDataFactory.CreateStoreContext();
        TestDataFactory.SeedCatalog(context);
        var repository = new GenericRepository<Product>(context);

        var result = await repository.GetAllWitSpecAsync(new ProductSpecification(new ProductSpecParams
        {
            Search = "alpha"
        }));

        result.Should().ContainSingle();
        result.Single().Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task SortByPriceDesc_ShouldReturnHighestPriceFirst()
    {
        await using var context = TestDataFactory.CreateStoreContext();
        TestDataFactory.SeedCatalog(context);
        var repository = new GenericRepository<Product>(context);

        var result = await repository.GetAllWitSpecAsync(new ProductSpecification(new ProductSpecParams
        {
            Sort = "PriceDesc"
        }));

        result.First().Name.Should().Be("Delta");
    }

    [Fact]
    public async Task Pagination_ShouldReturnSecondPage()
    {
        await using var context = TestDataFactory.CreateStoreContext();
        TestDataFactory.SeedCatalog(context);
        var repository = new GenericRepository<Product>(context);

        var result = await repository.GetAllWitSpecAsync(new ProductSpecification(new ProductSpecParams
        {
            PageIndex = 2,
            PageSize = 2,
            Sort = "Name"
        }));

        result.Should().HaveCount(2);
        result.Select(p => p.Name).Should().ContainInOrder("Delta", "Gamma");
    }

    [Fact]
    public async Task FilterByBrandAndType_ShouldReturnMatchingProducts()
    {
        await using var context = TestDataFactory.CreateStoreContext();
        TestDataFactory.SeedCatalog(context);
        var repository = new GenericRepository<Product>(context);

        var result = await repository.GetAllWitSpecAsync(new ProductSpecification(new ProductSpecParams
        {
            BrandId = 2,
            TypeId = 2
        }));

        result.Should().ContainSingle();
        result.Single().Name.Should().Be("Gamma");
    }
}
