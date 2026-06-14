using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Talabat.APIs.Controllers;
using Talabat.Core.Entities;
using Talabat.Repository.Data;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Controllers;

public class BuggyControllerTests
{
    [Fact]
    public void GetNotFoundRequest_ShouldReturnNotFound()
    {
        using var context = TestDataFactory.CreateStoreContext();
        var controller = new BuggyController(context);

        var response = controller.GetNotFoundRequest();

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void GetBadRequest_ShouldReturnBadRequest()
    {
        using var context = TestDataFactory.CreateStoreContext();
        var controller = new BuggyController(context);

        var response = controller.GetBadRequest();

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void GetServerError_ShouldThrowWhenProductMissing()
    {
        using var context = TestDataFactory.CreateStoreContext();
        var controller = new BuggyController(context);

        Action act = () => controller.GetServerError();

        act.Should().Throw<NullReferenceException>();
    }
}
