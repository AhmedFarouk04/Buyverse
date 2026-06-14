using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stripe;
using Talabat.Core.Models;
using Talabat.Core.Services;

namespace Talabat.Service
{
    public class StripePaymentIntentService : IPaymentIntentService
    {
        public StripePaymentIntentService(IConfiguration configuration)
        {
            StripeConfiguration.ApiKey = configuration["StripeSettings:SecretKey"];
        }

        public async Task<PaymentIntentResult> CreateAsync(long amount)
        {
            var paymentIntentService = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" }
            };

            var paymentIntent = await paymentIntentService.CreateAsync(options);
            return new PaymentIntentResult
            {
                Id = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret
            };
        }

        public async Task<PaymentIntentResult> UpdateAsync(string paymentIntentId, long amount)
        {
            var paymentIntentService = new PaymentIntentService();
            var options = new PaymentIntentUpdateOptions
            {
                Amount = amount
            };

            var paymentIntent = await paymentIntentService.UpdateAsync(paymentIntentId, options);
            return new PaymentIntentResult
            {
                Id = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret
            };
        }
    }
}
