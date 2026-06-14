using System;

namespace Talabat.Core.Models
{
    public class OrderSummary
    {
        public int TotalOrders { get; set; }

        public int PendingOrders { get; set; }

        public int PaymentReceivedOrders { get; set; }

        public int PaymentFailedOrders { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTimeOffset? LatestOrderDate { get; set; }
    }
}
