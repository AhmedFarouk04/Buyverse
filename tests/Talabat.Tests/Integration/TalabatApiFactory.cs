using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StackExchange.Redis;
using Talabat.Core.Entities;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Repositories;
using Talabat.Core.Services;
using Talabat.Repository.Data;
using Talabat.Repository.Identity;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Integration;

public sealed class TalabatApiFactory : WebApplicationFactory<Talabat.APIs.Program>
{
    private readonly string storeDatabaseName = $"store-{Guid.NewGuid():N}";
    private readonly string identityDatabaseName = $"identity-{Guid.NewGuid():N}";
    private readonly InMemoryBasketRepository basketRepository = new();

    public IBasketRepository BasketRepository => basketRepository;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "https://cdn.test/",
                ["FrontUrl"] = "https://frontend.test",
                ["JWT:Key"] = "integration-test-signing-key-123456",
                ["JWT:ValidIssuer"] = "integration-tests",
                ["JWT:ValidAudience"] = "integration-tests",
                ["StripeSettings:SecretKey"] = "sk_test_integration",
                ["StripeSettings:WebhookSecret"] = "whsec_test_integration"
            };

            configuration.AddInMemoryCollection(values!);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<StoreContext>));
            services.RemoveAll(typeof(DbContextOptions<AppIdentityDbContext>));
            services.RemoveAll<StoreContext>();
            services.RemoveAll<AppIdentityDbContext>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IBasketRepository>();
            services.RemoveAll<IPaymentService>();

            services.AddDbContext<StoreContext>(options =>
            {
                options.UseInMemoryDatabase(storeDatabaseName);
            });

            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseInMemoryDatabase(identityDatabaseName);
            });

            services.AddSingleton(Mock.Of<IConnectionMultiplexer>());
            services.AddSingleton<IBasketRepository>(basketRepository);
            services.AddSingleton<IPaymentService>(_ => new FakePaymentService(basketRepository));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });

            services.AddAuthorization();
        });
    }

    public async Task ResetStoreAsync(IEnumerable<Talabat.Core.Entities.Order_Aggregation.Order>? orders = null)
    {
        using var scope = Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<StoreContext>();
        await store.Database.EnsureDeletedAsync();
        await store.Database.EnsureCreatedAsync();

        TestDataFactory.SeedCatalog(store);

        if (orders is not null)
        {
            var deliveryMethods = await store.DeliveryMethods.ToDictionaryAsync(deliveryMethod => deliveryMethod.Id);

            foreach (var order in orders)
            {
                order.DeliveryMethod = deliveryMethods[order.DeliveryMethod.Id];
            }

            store.Orders.AddRange(orders);
            await store.SaveChangesAsync();
        }

        basketRepository.Clear();
    }

    public void SeedBasket(CustomerBasket basket)
    {
        basketRepository.Seed(basket);
    }
}
