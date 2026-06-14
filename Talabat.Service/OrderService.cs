using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Talabat.Core;
using Talabat.Core.Entities;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Models;
using Talabat.Core.Repositories;
using Talabat.Core.Services;
using Talabat.Core.Specifications.Order_Spec;

namespace Talabat.Service
{
    public class OrderService : IOrderService
    {
        private readonly IBasketRepository basketRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;

        public OrderService(
            IBasketRepository basketRepository,
            IUnitOfWork unitOfWork,
            IPaymentService paymentService)
        {
            this.basketRepository = basketRepository;
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
        }

        public async Task<Order?> CreateOrderAsync(
            string buyerEmail,
            string basketId,
            Address shippingAddress,
            int deliveryMethodId)
        {
            var basket = await basketRepository.GetBasketAsync(basketId);
            if (basket == null)
                throw new Exception($"Basket with Id {basketId} not found");

            var orderItems = new List<OrderItem>();
            if (basket?.Items?.Count > 0)
            {
                foreach (var item in basket.Items)
                {
                    var product = await _unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                    if (product == null)
                        throw new Exception($"Product with Id {item.Id} not found");

                    var productItemOrdered = new ProductOrderItem(
                        product.Id,
                        product.Name,
                        product.PictureUrl);

                    orderItems.Add(new OrderItem(
                        productItemOrdered,
                        product.Price,
                        item.Quantity));
                }
            }

            var subTotal = orderItems.Sum(item => item.Price * item.Quantity);

            var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>()
                .GetByIdAsync(deliveryMethodId);
            if (deliveryMethod == null)
                throw new Exception($"DeliveryMethod with Id {deliveryMethodId} not found");

            var spec = new OrderWithPaymentIntentSpecification(basket.PaymentIntentId);
            var existOrder = await _unitOfWork.Repository<Order>().GetByIdWitSpecAsync(spec);
            if (existOrder != null)
            {
                _unitOfWork.Repository<Order>().Delete(existOrder);
                await _paymentService.CreateOrUpdatePaymentIntent(basket.Id);
            }

            var order = new Order(
                buyerEmail,
                shippingAddress,
                deliveryMethod,
                orderItems,
                subTotal,
                basket.PaymentIntentId);

            await _unitOfWork.Repository<Order>().Add(order);

            var result = await _unitOfWork.Complete();
            if (result <= 0) return null;

            return order;
        }

        public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
        {
            var spec = new OrderSpecification(buyerEmail);

            return await _unitOfWork.Repository<Order>()
                .GetAllWitSpecAsync(spec);
        }

        public async Task<Order?> GetOrderByIdForUserAsync(int orderId, string buyerEmail)
        {
            var spec = new OrderSpecification(orderId, buyerEmail);

            return await _unitOfWork.Repository<Order>()
                .GetByIdWitSpecAsync(spec);
        }

        public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
        {
            return await _unitOfWork.Repository<DeliveryMethod>().GetAllAsync();
        }

        public async Task<OrderSummary> GetOrderSummaryForUserAsync(string buyerEmail)
        {
            var orders = await GetOrdersForUserAsync(buyerEmail);

            return new OrderSummary
            {
                TotalOrders = orders.Count,
                PendingOrders = orders.Count(order => order.Status == OrderStatus.pending),
                PaymentReceivedOrders = orders.Count(order => order.Status == OrderStatus.PaymentReceieved),
                PaymentFailedOrders = orders.Count(order => order.Status == OrderStatus.PaymentFailed),
                TotalSpent = orders.Sum(order => order.GetTotal()),
                LatestOrderDate = orders.Any() ? orders.Max(order => order.OrderDate) : null
            };
        }
    }
}
