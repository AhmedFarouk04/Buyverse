using FluentAssertions;
using Talabat.Core.Entities;
using Talabat.Repository;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Repository;

public class UnitOfWorkTests
{
    [Fact]
    public void Repository_ShouldCacheInstancesPerEntityType()
    {
        using var context = TestDataFactory.CreateStoreContext();
        var unitOfWork = TestDataFactory.CreateUnitOfWork(context);

        var productsRepo1 = unitOfWork.Repository<Product>();
        var productsRepo2 = unitOfWork.Repository<Product>();
        var brandsRepo = unitOfWork.Repository<ProductBrand>();

        productsRepo1.Should().BeSameAs(productsRepo2);
        productsRepo1.Should().NotBeSameAs(brandsRepo);
    }
}
