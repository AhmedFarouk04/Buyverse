using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Talabat.Core;
using Talabat.Core.Entities;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Repositories;
using Talabat.Core.Services;
using Talabat.Core.Specifications.Order_Spec;
using Product = Talabat.Core.Entities.Product;

namespace Talabat.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IBasketRepository _basketRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentIntentService _paymentIntentService;

        public PaymentService(
            IBasketRepository basketRepository,
            IUnitOfWork unitOfWork,
            IPaymentIntentService paymentIntentService)
        {
            _basketRepository = basketRepository;
            _unitOfWork = unitOfWork;
            _paymentIntentService = paymentIntentService;
        }

        public async Task<CustomerBasket?> CreateOrUpdatePaymentIntent(string basketId)
        {
            var basket = await _basketRepository.GetBasketAsync(basketId);
            if (basket == null) return null;

            var shippingPrice = 0m;
            if (basket.DeliveryMethodsId.HasValue)
            {
                var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>()
                    .GetByIdAsync(basket.DeliveryMethodsId.Value);

                if (deliveryMethod == null)
                    throw new System.Exception($"DeliveryMethod with Id {basket.DeliveryMethodsId.Value} not found");

                shippingPrice = deliveryMethod.Cost;
                basket.ShippingCost = deliveryMethod.Cost;
            }

            var basketItems = basket.Items ?? new List<BasketItem>();
            foreach (var item in basketItems)
            {
                var product = await _unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                if (product == null)
                    throw new System.Exception($"Product with Id {item.Id} not found");

                if (item.Price != product.Price)
                    item.Price = product.Price;
            }

            var amount = (long)((basketItems.Sum(item => item.Price * item.Quantity) + shippingPrice) * 100);

            if (string.IsNullOrEmpty(basket.PaymentIntentId))
            {
                var paymentIntent = await _paymentIntentService.CreateAsync(amount);
                basket.PaymentIntentId = paymentIntent.Id;
                basket.ClientSecret = paymentIntent.ClientSecret;
            }
            else
            {
                await _paymentIntentService.UpdateAsync(basket.PaymentIntentId, amount);
            }

            await _basketRepository.UpdateBasketAsync(basket);
            return basket;
        }

        public async Task<Order> UpdatePaymentIntentToSucceededOrFaild(string IntentId, bool isSucceeded)
        {
            var spec = new OrderWithPaymentIntentSpecification(IntentId);

            var order = await _unitOfWork.Repository<Order>()
                .GetByIdWitSpecAsync(spec);

            if (isSucceeded)
                order.Status = OrderStatus.PaymentReceieved;
            else
                order.Status = OrderStatus.PaymentFailed;

            _unitOfWork.Repository<Order>().Update(order);

            await _unitOfWork.Complete();

            return order;
        }
    }
}
