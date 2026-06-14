using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Talabat.APIs.Controllers;
using Talabat.APIs.Dtos;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Models;
using Talabat.Core.Services;
using OrderAddress = Talabat.Core.Entities.Order_Aggregation.Address;

namespace Talabat.Tests.Controllers;

public class OrdersControllerTests
{
    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequestWhenServiceReturnsNull()
    {
        var orderService = new Mock<IOrderService>();
        orderService.Setup(s => s.CreateOrderAsync("buyer@test.com", "basket-1", It.IsAny<OrderAddress>(), 1))
            .ReturnsAsync((Order)null!);

        var mapper = new Mock<AutoMapper.IMapper>();
        mapper.Setup(m => m.Map<AddressDto, OrderAddress>(It.IsAny<AddressDto>()))
            .Returns(new OrderAddress { FirstName = "A", LastName = "B", Street = "S", City = "C", Country = "EG" });

        var controller = new OrdersController(orderService.Object, mapper.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Email, "buyer@test.com")
                    }))
                }
            }
        };

        var response = await controller.CreateOrder(new OrderDto
        {
            BasketId = "basket-1",
            DeliveryMethodId = 1,
            ShippingAddress = new AddressDto
            {
                FirstName = "A",
                LastName = "B",
                Street = "S",
                City = "C",
                Country = "EG"
            }
        });

        response.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetOrdersForUser_ShouldReturnMappedOrders()
    {
        var orders = new List<Order>
        {
            Talabat.Tests.TestSupport.TestDataFactory.CreateOrder(1, "buyer@test.com", 20m),
            Talabat.Tests.TestSupport.TestDataFactory.CreateOrder(2, "buyer@test.com", 30m)
        };

        var mapped = new List<OrderToReturnDto>
        {
            new() { Id = 1, BuyerEmail = "buyer@test.com" },
            new() { Id = 2, BuyerEmail = "buyer@test.com" }
        };

        var orderService = new Mock<IOrderService>();
        orderService.Setup(s => s.GetOrdersForUserAsync("buyer@test.com")).ReturnsAsync(orders);

        var mapper = new Mock<AutoMapper.IMapper>();
        mapper.Setup(m => m.Map<IReadOnlyList<Order>, IReadOnlyList<OrderToReturnDto>>(It.IsAny<IReadOnlyList<Order>>()))
            .Returns(mapped);

        var controller = new OrdersController(orderService.Object, mapper.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Email, "buyer@test.com")
                    }))
                }
            }
        };

        var response = await controller.GetOrdersForUser();

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeAssignableTo<IReadOnlyList<OrderToReturnDto>>().Subject;
        payload.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderSummary_ShouldReturnSummary()
    {
        var orderService = new Mock<IOrderService>();
        orderService.Setup(s => s.GetOrderSummaryForUserAsync("buyer@test.com")).ReturnsAsync(new OrderSummary
        {
            TotalOrders = 3,
            PendingOrders = 1,
            PaymentReceivedOrders = 1,
            PaymentFailedOrders = 1,
            TotalSpent = 75m
        });

        var controller = new OrdersController(orderService.Object, Mock.Of<AutoMapper.IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Email, "buyer@test.com")
                    }))
                }
            }
        };

        var response = await controller.GetOrderSummary();

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = ok.Value.Should().BeOfType<OrderSummary>().Subject;
        summary.TotalOrders.Should().Be(3);
        summary.TotalSpent.Should().Be(75m);
    }

    [Fact]
    public async Task GetOrderForUser_ShouldReturnMappedOrder()
    {
        var order = Talabat.Tests.TestSupport.TestDataFactory.CreateOrder(7, "buyer@test.com", 50m);
        var mapped = new OrderToReturnDto
        {
            Id = 7,
            BuyerEmail = "buyer@test.com",
            Total = 55m
        };

        var orderService = new Mock<IOrderService>();
        orderService.Setup(s => s.GetOrderByIdForUserAsync(7, "buyer@test.com")).ReturnsAsync(order);

        var mapper = new Mock<AutoMapper.IMapper>();
        mapper.Setup(m => m.Map<Order, OrderToReturnDto>(order)).Returns(mapped);

        var controller = new OrdersController(orderService.Object, mapper.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Email, "buyer@test.com")
                    }))
                }
            }
        };

        var response = await controller.GetOrderForUser(7);

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<OrderToReturnDto>().Which.Id.Should().Be(7);
    }

    [Fact]
    public async Task GetDeliveryMethods_ShouldReturnItems()
    {
        var orderService = new Mock<IOrderService>();
        orderService.Setup(s => s.GetDeliveryMethodsAsync()).ReturnsAsync(new List<DeliveryMethod>
        {
            new("Fast", "Fast delivery", 10m, "1-2 days") { Id = 1 },
            new("Slow", "Slow delivery", 0m, "3-5 days") { Id = 2 }
        });

        var controller = new OrdersController(orderService.Object, Mock.Of<AutoMapper.IMapper>());

        var response = await controller.GetDeliveryMethids();

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<DeliveryMethod>>()
            .Subject.Should().HaveCount(2);
    }
}
