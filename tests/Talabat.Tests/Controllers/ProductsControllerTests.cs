using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Talabat.APIs.Controllers;
using Talabat.APIs.Dtos;
using Talabat.APIs.Helper;
using Talabat.Core;
using Talabat.Core.Entities;
using Talabat.Core.Repositories;
using Talabat.Core.Specifications;

namespace Talabat.Tests.Controllers;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetProducts_ShouldReturnPagedResult()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Alpha", Description = "A", PictureUrl = "a.png", Price = 10m, ProductBrandId = 1, ProductTypeId = 1 },
            new() { Id = 2, Name = "Beta", Description = "B", PictureUrl = "b.png", Price = 20m, ProductBrandId = 1, ProductTypeId = 1 }
        };

        var mapped = new List<ProductToReturnDto>
        {
            new() { Id = 1, Name = "Alpha", Description = "A", PictureUrl = "a.png", Price = 10m, ProductBrandId = 1, ProductBrand = "Brand 1", ProductTypeId = 1, ProductType = "Type 1" },
            new() { Id = 2, Name = "Beta", Description = "B", PictureUrl = "b.png", Price = 20m, ProductBrandId = 1, ProductBrand = "Brand 1", ProductTypeId = 1, ProductType = "Type 1" }
        };

        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo.Setup(r => r.GetAllWitSpecAsync(It.IsAny<ISpecification<Product>>())).ReturnsAsync(products);
        productRepo.Setup(r => r.GetCountWithSpecAsync(It.IsAny<ISpecification<Product>>())).ReturnsAsync(4);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Product>()).Returns(productRepo.Object);

        var mapper = new Mock<AutoMapper.IMapper>();
        mapper.Setup(m => m.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(It.IsAny<IReadOnlyList<Product>>()))
            .Returns(mapped);

        var controller = new ProductsController(mapper.Object, unitOfWork.Object);

        var response = await controller.GetProducts(new ProductSpecParams
        {
            PageIndex = 1,
            PageSize = 2
        });

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagination = ok.Value.Should().BeOfType<Pagination<ProductToReturnDto>>().Subject;

        pagination.PageIndex.Should().Be(1);
        pagination.PageSize.Should().Be(2);
        pagination.Count.Should().Be(4);
        pagination.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProduct_ShouldReturnNotFoundWhenMissing()
    {
        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo.Setup(r => r.GetByIdWitSpecAsync(It.IsAny<ISpecification<Product>>())).ReturnsAsync((Product)null!);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Product>()).Returns(productRepo.Object);

        var mapper = new Mock<AutoMapper.IMapper>();
        var controller = new ProductsController(mapper.Object, unitOfWork.Object);

        var response = await controller.GetProduct(42);

        response.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAllBrands_ShouldReturnBrands()
    {
        var brandRepo = new Mock<IGenericRepository<ProductBrand>>();
        brandRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ProductBrand>
        {
            new() { Id = 1, Name = "Brand 1" },
            new() { Id = 2, Name = "Brand 2" }
        });

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<ProductBrand>()).Returns(brandRepo.Object);

        var controller = new ProductsController(Mock.Of<AutoMapper.IMapper>(), unitOfWork.Object);

        var response = await controller.GetAllBrands();

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<ProductBrand>>()
            .Subject.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllTypes_ShouldReturnTypes()
    {
        var typeRepo = new Mock<IGenericRepository<ProductType>>();
        typeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ProductType>
        {
            new() { Id = 1, Name = "Type 1" },
            new() { Id = 2, Name = "Type 2" }
        });

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<ProductType>()).Returns(typeRepo.Object);

        var controller = new ProductsController(Mock.Of<AutoMapper.IMapper>(), unitOfWork.Object);

        var response = await controller.GetAllTypes();

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<ProductType>>()
            .Subject.Should().HaveCount(2);
    }
}
