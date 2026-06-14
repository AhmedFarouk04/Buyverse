using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Talabat.Core.Entities.Order_Aggregation;
using Talabat.Core.Models;

namespace Talabat.Core.Services
{
    public  interface IOrderService
    {

        Task<Order?> CreateOrderAsync(string BuyserEmail, string basketId, Address ShipppingAddress, int deliveryMethodId);
        Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail);
        Task<Order?> GetOrderByIdForUserAsync(int orderid, string buyerEmail);
        Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync();
        Task<OrderSummary> GetOrderSummaryForUserAsync(string buyerEmail);
    } 
}
