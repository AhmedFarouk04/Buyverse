using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Talabat.APIs.Dtos;
using Talabat.APIs.Helper;
using Talabat.Core.Entities;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Mappings;

public class MappingProfilesTests
{
    [Fact]
    public void ShouldMapProductPictureUrlUsingApiBaseUrl()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TestDataFactory.CreateConfiguration(new Dictionary<string, string>
        {
            ["ApiBaseUrl"] = "https://localhost:5001/"
        }));
        services.AddAutoMapper(typeof(MappingProfiles));

        var mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

        var dto = mapper.Map<ProductToReturnDto>(new Product
        {
            Id = 1,
            Name = "Phone",
            Description = "Desc",
            PictureUrl = "images/phone.png",
            Price = 10m,
            ProductBrand = new ProductBrand { Id = 1, Name = "Brand" },
            ProductType = new ProductType { Id = 1, Name = "Type" }
        });

        dto.PictureUrl.Should().Be("https://localhost:5001/images/phone.png");
        dto.ProductBrand.Should().Be("Brand");
        dto.ProductType.Should().Be("Type");
    }

    [Fact]
    public void ShouldMapOrderItemPictureUrlUsingApiBaseUrl()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TestDataFactory.CreateConfiguration(new Dictionary<string, string>
        {
            ["ApiBaseUrl"] = "https://localhost:5001/"
        }));
        services.AddAutoMapper(typeof(MappingProfiles));

        var mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

        var dto = mapper.Map<OrderItemDto>(new OrderItem(
            new ProductOrderItem(1, "Phone", "images/phone.png"),
            10m,
            2));

        dto.PictureUrl.Should().Be("https://localhost:5001/images/phone.png");
        dto.ProductId.Should().Be(1);
        dto.ProductName.Should().Be("Phone");
    }
}
