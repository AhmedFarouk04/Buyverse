using FluentAssertions;
using Talabat.Core.Entities;
using Talabat.Core.Specifications;
using Talabat.Repository;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Repository;

public class GenericRepositoryTests
{
    [Fact]
    public async Task AddAndGetById_ShouldPersistEntity()
    {
        await using var context = TestDataFactory.CreateStoreContext();
        var repository = new GenericRepository<Product>(context);
        var product = TestDataFactory.CreateProduct(10, "Headphones", 99m);

        await repository.Add(product);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(10);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Headphones");
    }

    [Fact]
    public async Task Update_ShouldPersistChanges()
    {
        await using var context = TestDataFactory.CreateStoreContext();
        context.Products.Add(TestDataFactory.CreateProduct(11, "Monitor", 120m));
        await context.SaveChangesAsync();

        var repository = new GenericRepository<Product>(context);
        var product = await repository.GetByIdAsync(11);
        product!.Price = 150m;

        repository.Update(product);
        await context.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(11);
        updated!.Price.Should().Be(150m);
    }

    [Fact]
    public async Task Delete_ShouldRemoveEntity()
    {
        await using var context = TestDataFactory.CreateStoreContext();
        context.Products.Add(TestDataFactory.CreateProduct(12, "Keyboard", 45m));
        await context.SaveChangesAsync();

        var repository = new GenericRepository<Product>(context);
        var product = await repository.GetByIdAsync(12);

        repository.Delete(product!);
        await context.SaveChangesAsync();

        (await repository.GetByIdAsync(12)).Should().BeNull();
    }

    [Fact]
    public async Task GetCountWithSpec_ShouldReturnFilteredCount()
    {
        await using var context = TestDataFactory.CreateStoreContext();
        TestDataFactory.SeedCatalog(context);
        var repository = new GenericRepository<Product>(context);

        var count = await repository.GetCountWithSpecAsync(new ProductWithFilterationForCountSpecification(new ProductSpecParams
        {
            BrandId = 1
        }));

        count.Should().Be(2);
    }
}
