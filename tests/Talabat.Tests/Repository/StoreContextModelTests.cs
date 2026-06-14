using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Repository.Data;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Repository;

public class StoreContextModelTests
{
    [Fact]
    public void OrderConfiguration_ShouldOwnShippingAddressAndRestrictDeliveryMethodDelete()
    {
        using var context = TestDataFactory.CreateStoreContext();
        var orderType = context.Model.FindEntityType(typeof(Order));

        orderType.Should().NotBeNull();
        orderType!.FindNavigation(nameof(Order.ShippingAdress))!.ForeignKey.IsOwnership.Should().BeTrue();
        orderType.FindNavigation(nameof(Order.DeliveryMethod))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        orderType.FindNavigation(nameof(Order.Items))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void OrderItemConfiguration_ShouldOwnProductOrderItem()
    {
        using var context = TestDataFactory.CreateStoreContext();
        var orderItemType = context.Model.FindEntityType(typeof(OrderItem));

        orderItemType.Should().NotBeNull();
        orderItemType!.FindNavigation(nameof(OrderItem.ItemOrdered))!.ForeignKey.IsOwnership.Should().BeTrue();
    }
}
