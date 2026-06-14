using FluentAssertions;
using Moq;
using Talabat.Core;
using Talabat.Core.Entities;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Repositories;
using Talabat.Core.Services;
using Talabat.Core.Specifications;
using Talabat.Service;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Services;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrderAndDeleteExistingPaymentIntentOrder()
    {
        var basket = TestDataFactory.CreateBasket(
            "basket-1",
            [TestDataFactory.CreateBasketItem(1, 10m, 2)],
            paymentIntentId: "pi_123",
            deliveryMethodId: 1);

        var existingOrder = TestDataFactory.CreateOrder(99, "buyer@test.com", 20m, OrderStatus.pending, deliveryCost: 5m);
        existingOrder.PaymentIntentId = "pi_123";

        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(r => r.GetBasketAsync("basket-1")).ReturnsAsync(basket);

        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateProduct(1, "Phone", 10m));

        var deliveryRepo = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateDeliveryMethod(1, "Fast", 5m, "1-2 days"));

        var orderRepo = new Mock<IGenericRepository<Order>>();
        orderRepo.Setup(r => r.GetByIdWitSpecAsync(It.IsAny<ISpecification<Order>>())).ReturnsAsync(existingOrder);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Product>()).Returns(productRepo.Object);
        unitOfWork.Setup(u => u.Repository<DeliveryMethod>()).Returns(deliveryRepo.Object);
        unitOfWork.Setup(u => u.Repository<Order>()).Returns(orderRepo.Object);
        unitOfWork.Setup(u => u.Complete()).ReturnsAsync(1);

        var paymentService = new Mock<IPaymentService>();
        paymentService.Setup(s => s.CreateOrUpdatePaymentIntent("basket-1")).ReturnsAsync(basket);

        var service = new OrderService(basketRepository.Object, unitOfWork.Object, paymentService.Object);

        var result = await service.CreateOrderAsync(
            "buyer@test.com",
            "basket-1",
            new Address { FirstName = "A", LastName = "B", Street = "S", City = "C", Country = "EG" },
            1);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.SubTotal.Should().Be(20m);
        result.GetTotal().Should().Be(25m);
        orderRepo.Verify(r => r.Delete(existingOrder), Times.Once);
        paymentService.Verify(s => s.CreateOrUpdatePaymentIntent("basket-1"), Times.Once);
        basketRepository.Verify(r => r.UpdateBasketAsync(basket), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowWhenProductMissing()
    {
        var basket = TestDataFactory.CreateBasket(
            "basket-2",
            [TestDataFactory.CreateBasketItem(777, 10m, 1)],
            paymentIntentId: "pi_777",
            deliveryMethodId: 1);

        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(r => r.GetBasketAsync("basket-2")).ReturnsAsync(basket);

        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo.Setup(r => r.GetByIdAsync(777)).ReturnsAsync((Product)null!);

        var deliveryRepo = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateDeliveryMethod(1, "Fast", 5m, "1-2 days"));

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Product>()).Returns(productRepo.Object);
        unitOfWork.Setup(u => u.Repository<DeliveryMethod>()).Returns(deliveryRepo.Object);

        var service = new OrderService(basketRepository.Object, unitOfWork.Object, Mock.Of<IPaymentService>());

        var act = async () => await service.CreateOrderAsync(
            "buyer@test.com",
            "basket-2",
            new Address { FirstName = "A", LastName = "B", Street = "S", City = "C", Country = "EG" },
            1);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Product with Id 777 not found");
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowWhenDeliveryMethodMissing()
    {
        var basket = TestDataFactory.CreateBasket(
            "basket-3",
            [TestDataFactory.CreateBasketItem(1, 10m, 1)],
            paymentIntentId: "pi_3",
            deliveryMethodId: 999);

        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(r => r.GetBasketAsync("basket-3")).ReturnsAsync(basket);

        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateProduct(1, "Phone", 10m));

        var deliveryRepo = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((DeliveryMethod)null!);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Product>()).Returns(productRepo.Object);
        unitOfWork.Setup(u => u.Repository<DeliveryMethod>()).Returns(deliveryRepo.Object);

        var service = new OrderService(basketRepository.Object, unitOfWork.Object, Mock.Of<IPaymentService>());

        var act = async () => await service.CreateOrderAsync(
            "buyer@test.com",
            "basket-3",
            new Address { FirstName = "A", LastName = "B", Street = "S", City = "C", Country = "EG" },
            999);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("DeliveryMethod with Id 999 not found");
    }

    [Fact]
    public async Task GetOrderSummaryForUserAsync_ShouldReturnAggregates()
    {
        var orders = new List<Order>
        {
            TestDataFactory.CreateOrder(1, "buyer@test.com", 20m, OrderStatus.pending, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero)),
            TestDataFactory.CreateOrder(2, "buyer@test.com", 30m, OrderStatus.PaymentReceieved, new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero), 10m),
            TestDataFactory.CreateOrder(3, "buyer@test.com", 15m, OrderStatus.PaymentFailed, new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero), 0m)
        };

        var orderRepo = new Mock<IGenericRepository<Order>>();
        orderRepo.Setup(r => r.GetAllWitSpecAsync(It.IsAny<ISpecification<Order>>())).ReturnsAsync(orders);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Order>()).Returns(orderRepo.Object);

        var service = new OrderService(Mock.Of<IBasketRepository>(), unitOfWork.Object, Mock.Of<IPaymentService>());

        var summary = await service.GetOrderSummaryForUserAsync("buyer@test.com");

        summary.TotalOrders.Should().Be(3);
        summary.PendingOrders.Should().Be(1);
        summary.PaymentReceivedOrders.Should().Be(1);
        summary.PaymentFailedOrders.Should().Be(1);
        summary.TotalSpent.Should().Be(80m);
        summary.LatestOrderDate.Should().Be(new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetOrdersForUserAsync_ShouldReturnOrdersFromSpecification()
    {
        var orders = new List<Order>
        {
            TestDataFactory.CreateOrder(1, "buyer@test.com", 20m),
            TestDataFactory.CreateOrder(2, "buyer@test.com", 30m)
        };

        var orderRepo = new Mock<IGenericRepository<Order>>();
        orderRepo.Setup(r => r.GetAllWitSpecAsync(It.IsAny<ISpecification<Order>>())).ReturnsAsync(orders);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Order>()).Returns(orderRepo.Object);

        var service = new OrderService(Mock.Of<IBasketRepository>(), unitOfWork.Object, Mock.Of<IPaymentService>());

        var result = await service.GetOrdersForUserAsync("buyer@test.com");

        result.Should().HaveCount(2);
        result.First().BuyerEmail.Should().Be("buyer@test.com");
    }

    [Fact]
    public async Task GetOrderByIdForUserAsync_ShouldReturnOrderFromSpecification()
    {
        var order = TestDataFactory.CreateOrder(5, "buyer@test.com", 40m);
        var orderRepo = new Mock<IGenericRepository<Order>>();
        orderRepo.Setup(r => r.GetByIdWitSpecAsync(It.IsAny<ISpecification<Order>>())).ReturnsAsync(order);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Order>()).Returns(orderRepo.Object);

        var service = new OrderService(Mock.Of<IBasketRepository>(), unitOfWork.Object, Mock.Of<IPaymentService>());

        var result = await service.GetOrderByIdForUserAsync(5, "buyer@test.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
    }

    [Fact]
    public async Task GetDeliveryMethodsAsync_ShouldReturnMethods()
    {
        var methods = new List<DeliveryMethod>
        {
            TestDataFactory.CreateDeliveryMethod(1, "Fast", 10m, "1-2 days"),
            TestDataFactory.CreateDeliveryMethod(2, "Slow", 0m, "3-5 days")
        };

        var deliveryRepo = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(methods);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<DeliveryMethod>()).Returns(deliveryRepo.Object);

        var service = new OrderService(Mock.Of<IBasketRepository>(), unitOfWork.Object, Mock.Of<IPaymentService>());

        var result = await service.GetDeliveryMethodsAsync();

        result.Should().HaveCount(2);
    }
}
