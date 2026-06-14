using FluentAssertions;
using Moq;
using Talabat.Core;
using Talabat.Core.Entities;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Models;
using Talabat.Core.Repositories;
using Talabat.Core.Services;
using Talabat.Core.Specifications;
using Talabat.Service;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Services;

public class PaymentServiceTests
{
    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ShouldReturnNullWhenBasketMissing()
    {
        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(r => r.GetBasketAsync("missing")).ReturnsAsync((CustomerBasket)null!);

        var service = new PaymentService(
            basketRepository.Object,
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IPaymentIntentService>());

        var result = await service.CreateOrUpdatePaymentIntent("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ShouldCreateIntentAndRepriceBasket()
    {
        var basket = TestDataFactory.CreateBasket(
            "basket-1",
            [TestDataFactory.CreateBasketItem(1, 10m, 2)],
            deliveryMethodId: 1);

        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(r => r.GetBasketAsync("basket-1")).ReturnsAsync(basket);
        basketRepository.Setup(r => r.UpdateBasketAsync(basket)).ReturnsAsync(basket);

        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateProduct(1, "Phone", 15m));

        var deliveryRepo = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateDeliveryMethod(1, "Fast", 5m, "1-2 days"));

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Product>()).Returns(productRepo.Object);
        unitOfWork.Setup(u => u.Repository<DeliveryMethod>()).Returns(deliveryRepo.Object);

        var paymentIntentService = new Mock<IPaymentIntentService>();
        paymentIntentService
            .Setup(s => s.CreateAsync(3500))
            .ReturnsAsync(new PaymentIntentResult { Id = "pi_new", ClientSecret = "secret" });

        var service = new PaymentService(
            basketRepository.Object,
            unitOfWork.Object,
            paymentIntentService.Object);

        var result = await service.CreateOrUpdatePaymentIntent("basket-1");

        result.Should().NotBeNull();
        result!.PaymentIntentId.Should().Be("pi_new");
        result.ClientSecret.Should().Be("secret");
        result.Items.Should().ContainSingle();
        result.Items[0].Price.Should().Be(15m);
        result.ShippingCost.Should().Be(5m);
        paymentIntentService.Verify(s => s.CreateAsync(3500), Times.Once);
        basketRepository.Verify(r => r.UpdateBasketAsync(basket), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ShouldUpdateIntentWhenBasketAlreadyHasIntent()
    {
        var basket = TestDataFactory.CreateBasket(
            "basket-2",
            [TestDataFactory.CreateBasketItem(1, 10m, 1)],
            paymentIntentId: "pi_existing",
            deliveryMethodId: 1);

        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(r => r.GetBasketAsync("basket-2")).ReturnsAsync(basket);
        basketRepository.Setup(r => r.UpdateBasketAsync(basket)).ReturnsAsync(basket);

        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateProduct(1, "Phone", 10m));

        var deliveryRepo = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateDeliveryMethod(1, "Fast", 5m, "1-2 days"));

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Product>()).Returns(productRepo.Object);
        unitOfWork.Setup(u => u.Repository<DeliveryMethod>()).Returns(deliveryRepo.Object);

        var paymentIntentService = new Mock<IPaymentIntentService>();
        paymentIntentService
            .Setup(s => s.UpdateAsync("pi_existing", 1500))
            .ReturnsAsync(new PaymentIntentResult { Id = "pi_existing", ClientSecret = "secret" });

        var service = new PaymentService(
            basketRepository.Object,
            unitOfWork.Object,
            paymentIntentService.Object);

        var result = await service.CreateOrUpdatePaymentIntent("basket-2");

        result.Should().NotBeNull();
        paymentIntentService.Verify(s => s.UpdateAsync("pi_existing", 1500), Times.Once);
        result!.PaymentIntentId.Should().Be("pi_existing");
    }

    [Fact]
    public async Task UpdatePaymentIntentToSucceededOrFaild_ShouldChangeOrderStatus()
    {
        var order = TestDataFactory.CreateOrder(1, "buyer@test.com", 20m, OrderStatus.pending);
        var orderRepo = new Mock<IGenericRepository<Order>>();
        orderRepo.Setup(r => r.GetByIdWitSpecAsync(It.IsAny<ISpecification<Order>>()))
            .ReturnsAsync(order);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Order>()).Returns(orderRepo.Object);
        unitOfWork.Setup(u => u.Complete()).ReturnsAsync(1);

        var service = new PaymentService(
            Mock.Of<IBasketRepository>(),
            unitOfWork.Object,
            Mock.Of<IPaymentIntentService>());

        var success = await service.UpdatePaymentIntentToSucceededOrFaild("pi_1", true);
        success.Status.Should().Be(OrderStatus.PaymentReceieved);

        var failed = await service.UpdatePaymentIntentToSucceededOrFaild("pi_1", false);
        failed.Status.Should().Be(OrderStatus.PaymentFailed);
        orderRepo.Verify(r => r.Update(order), Times.Exactly(2));
        unitOfWork.Verify(u => u.Complete(), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ShouldThrowWhenDeliveryMethodMissing()
    {
        var basket = TestDataFactory.CreateBasket(
            "basket-3",
            [TestDataFactory.CreateBasketItem(1, 10m, 1)],
            deliveryMethodId: 9);

        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(r => r.GetBasketAsync("basket-3")).ReturnsAsync(basket);

        var deliveryRepo = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((DeliveryMethod)null!);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<DeliveryMethod>()).Returns(deliveryRepo.Object);

        var service = new PaymentService(
            basketRepository.Object,
            unitOfWork.Object,
            Mock.Of<IPaymentIntentService>());

        var act = async () => await service.CreateOrUpdatePaymentIntent("basket-3");

        await act.Should().ThrowAsync<System.Exception>()
            .WithMessage("DeliveryMethod with Id 9 not found");
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ShouldThrowWhenProductMissing()
    {
        var basket = TestDataFactory.CreateBasket(
            "basket-4",
            [TestDataFactory.CreateBasketItem(77, 10m, 1)],
            deliveryMethodId: 1);

        var basketRepository = new Mock<IBasketRepository>();
        basketRepository.Setup(r => r.GetBasketAsync("basket-4")).ReturnsAsync(basket);

        var productRepo = new Mock<IGenericRepository<Product>>();
        productRepo.Setup(r => r.GetByIdAsync(77)).ReturnsAsync((Product)null!);

        var deliveryRepo = new Mock<IGenericRepository<DeliveryMethod>>();
        deliveryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.CreateDeliveryMethod(1, "Fast", 5m, "1-2 days"));

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<Product>()).Returns(productRepo.Object);
        unitOfWork.Setup(u => u.Repository<DeliveryMethod>()).Returns(deliveryRepo.Object);

        var service = new PaymentService(
            basketRepository.Object,
            unitOfWork.Object,
            Mock.Of<IPaymentIntentService>());

        var act = async () => await service.CreateOrUpdatePaymentIntent("basket-4");

        await act.Should().ThrowAsync<System.Exception>()
            .WithMessage("Product with Id 77 not found");
    }
}
