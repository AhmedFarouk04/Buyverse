namespace Talabat.Core.Models
{
    public class PaymentIntentResult
    {
        public required string Id { get; set; }

        public required string ClientSecret { get; set; }
    }
}
