using Talabat.Core.Entities;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Repositories;
using Talabat.Core.Services;

namespace Talabat.Tests.Integration;

internal sealed class FakePaymentService : IPaymentService
{
    private readonly IBasketRepository baskets;

    public FakePaymentService(IBasketRepository baskets)
    {
        this.baskets = baskets;
    }

    public async Task<CustomerBasket?> CreateOrUpdatePaymentIntent(string basketid)
    {
        CustomerBasket? basket = await baskets.GetBasketAsync(basketid);
        return basket;
    }

    public Task<Order> UpdatePaymentIntentToSucceededOrFaild(string intentId, bool isSucceeded)
    {
        return Task.FromException<Order>(
            new NotSupportedException(
                $"Webhook handling is not part of the integration test double: {intentId}, {isSucceeded}"));
    }
}
