using System.Collections.Concurrent;
using Talabat.Core.Entities;
using Talabat.Core.Repositories;

namespace Talabat.Tests.Integration;

internal sealed class InMemoryBasketRepository : IBasketRepository
{
    private readonly ConcurrentDictionary<string, CustomerBasket> baskets = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> DeleteBasketAsync(string basketId)
    {
        return Task.FromResult(baskets.TryRemove(basketId, out _));
    }

    public Task<CustomerBasket> GetBasketAsync(string basketId)
    {
        baskets.TryGetValue(basketId, out var basket);
        return Task.FromResult(basket!);
    }

    public Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        baskets[basket.Id] = basket;
        return Task.FromResult(basket);
    }

    public void Seed(CustomerBasket basket) => baskets[basket.Id] = basket;

    public void Clear() => baskets.Clear();
}
