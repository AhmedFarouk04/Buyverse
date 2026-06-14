using FluentAssertions;
using Talabat.Core.Specifications;

namespace Talabat.Tests.Models;

public class ProductSpecParamsTests
{
    [Fact]
    public void PageSize_ShouldClampToMaxPageSize()
    {
        var spec = new ProductSpecParams
        {
            PageSize = 50
        };

        spec.PageSize.Should().Be(10);
    }

    [Fact]
    public void Search_ShouldNormalizeToLowerCase()
    {
        var spec = new ProductSpecParams
        {
            Search = "HeAdPhOnEs"
        };

        spec.Search.Should().Be("headphones");
    }
}
