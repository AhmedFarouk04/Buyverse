using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq;
using Talabat.Core;
using Talabat.Core.Entities;
using Talabat.Core.Entities.Identity;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Repository;
using Talabat.Repository.Data;
using OrderAddress = Talabat.Core.Entities.Order_Aggregation.Address;

namespace Talabat.Tests.TestSupport;

internal static class TestDataFactory
{
    public static IConfiguration CreateConfiguration(IDictionary<string, string>? values = null)
    {
        var initialData = (values ?? new Dictionary<string, string>())
            .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value));

        return new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();
    }

    public static StoreContext CreateStoreContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new StoreContext(options);
    }

    public static UnitOfWork CreateUnitOfWork(StoreContext context) => new(context);

    public static void SeedCatalog(StoreContext context)
    {
        context.Products.AddRange(
            CreateProduct(1, "Alpha", 10m, 1, 1),
            CreateProduct(2, "Beta", 20m, 1, 1),
            CreateProduct(3, "Gamma", 30m, 2, 2),
            CreateProduct(4, "Delta", 40m, 2, 1));

        context.ProductBrands.AddRange(
            new ProductBrand { Id = 1, Name = "Brand One" },
            new ProductBrand { Id = 2, Name = "Brand Two" });

        context.ProductTypes.AddRange(
            new ProductType { Id = 1, Name = "Type One" },
            new ProductType { Id = 2, Name = "Type Two" });

        context.DeliveryMethods.AddRange(
            CreateDeliveryMethod(1, "Fast", 5m, "1-2 days"),
            CreateDeliveryMethod(2, "Normal", 0m, "3-5 days"));

        context.SaveChanges();
    }

    public static Product CreateProduct(
        int id,
        string name,
        decimal price,
        int brandId = 1,
        int typeId = 1)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Description = $"{name} description",
            PictureUrl = $"{name.ToLowerInvariant()}.png",
            Price = price,
            ProductBrandId = brandId,
            ProductTypeId = typeId
        };
    }

    public static DeliveryMethod CreateDeliveryMethod(int id, string shortName, decimal cost, string deliveryTime)
    {
        return new DeliveryMethod(shortName, $"{shortName} delivery", cost, deliveryTime)
        {
            Id = id
        };
    }

    public static CustomerBasket CreateBasket(
        string id,
        IEnumerable<BasketItem>? items = null,
        string? paymentIntentId = null,
        string? clientSecret = null,
        int? deliveryMethodId = null)
    {
        return new CustomerBasket(id)
        {
            Items = items?.ToList() ?? new List<BasketItem>(),
            PaymentIntentId = paymentIntentId ?? string.Empty,
            ClientSecret = clientSecret ?? string.Empty,
            DeliveryMethodsId = deliveryMethodId
        };
    }

    public static BasketItem CreateBasketItem(int id, decimal price, int quantity)
    {
        return new BasketItem
        {
            Id = id,
            ProductName = $"Product {id}",
            PictureUrl = $"product-{id}.png",
            Price = price,
            Quantity = quantity,
            Brand = $"Brand {id}"
        };
    }

    public static Order CreateOrder(
        int id,
        string buyerEmail,
        decimal subTotal,
        OrderStatus status = OrderStatus.pending,
        DateTimeOffset? orderDate = null,
        decimal deliveryCost = 5m)
    {
        var deliveryMethod = new DeliveryMethod("Standard", "Standard delivery", deliveryCost, "3-5 days")
        {
            Id = 1
        };

        var order = new Order(
            buyerEmail,
            new OrderAddress
            {
                FirstName = "F",
                LastName = "L",
                Street = "Street",
                City = "Cairo",
                Country = "Egypt"
            },
            deliveryMethod,
            new List<OrderItem>
            {
                new(new ProductOrderItem(id, $"Product {id}", $"product-{id}.png"), subTotal, 1)
            },
            subTotal,
            $"pi_{id}")
        {
            Id = id,
            Status = status,
            OrderDate = orderDate ?? DateTimeOffset.UtcNow
        };

        return order;
    }

    public static Mock<UserManager<AppUser>> CreateUserManagerMock(IList<string>? roles = null)
    {
        var store = new Mock<IUserStore<AppUser>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = Array.Empty<IUserValidator<AppUser>>();
        var passwordValidators = Array.Empty<IPasswordValidator<AppUser>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = Mock.Of<ILogger<UserManager<AppUser>>>();

        var userManager = new Mock<UserManager<AppUser>>(
            store.Object,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);

        userManager
            .Setup(m => m.GetRolesAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(roles ?? new List<string>());

        return userManager;
    }
}
