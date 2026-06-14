using System.Threading.Tasks;
using Talabat.Core.Models;

namespace Talabat.Core.Services
{
    public interface IPaymentIntentService
    {
        Task<PaymentIntentResult> CreateAsync(long amount);

        Task<PaymentIntentResult> UpdateAsync(string paymentIntentId, long amount);
    }
}
