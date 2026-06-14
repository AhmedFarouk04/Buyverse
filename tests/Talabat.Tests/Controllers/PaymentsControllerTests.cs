using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Talabat.APIs.Controllers;
using Talabat.APIs.Dtos;
using Talabat.Core.Entities;
using Talabat.Core.Services;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Controllers;

public class PaymentsControllerTests
{
    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ShouldReturnOkWhenBasketExists()
    {
        var paymentService = new Mock<IPaymentService>();
        paymentService.Setup(s => s.CreateOrUpdatePaymentIntent("basket-1")).ReturnsAsync(new CustomerBasket("basket-1"));

        var controller = new PaymentsController(
            paymentService.Object,
            Mock.Of<AutoMapper.IMapper>(),
            Mock.Of<ILogger<PaymentsController>>(),
            TestDataFactory.CreateConfiguration(new Dictionary<string, string>
            {
                ["StripeSettings:WebhookSecret"] = "whsec_test"
            }));

        var response = await controller.CreateOrUpdatePaymentIntent("basket-1");

        response.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_ShouldReturnBadRequestWhenBasketMissing()
    {
        var paymentService = new Mock<IPaymentService>();
        paymentService.Setup(s => s.CreateOrUpdatePaymentIntent("missing")).ReturnsAsync((CustomerBasket)null!);

        var controller = new PaymentsController(
            paymentService.Object,
            Mock.Of<AutoMapper.IMapper>(),
            Mock.Of<ILogger<PaymentsController>>(),
            TestDataFactory.CreateConfiguration(new Dictionary<string, string>
            {
                ["StripeSettings:WebhookSecret"] = "whsec_test"
            }));

        var response = await controller.CreateOrUpdatePaymentIntent("missing");

        response.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
