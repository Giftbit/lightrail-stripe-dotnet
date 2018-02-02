using Lightrail.Model;
using Newtonsoft.Json;
using Stripe;
using System;

namespace Lightrail.Stripe
{
    public class StripeLightrailSplitTenderCharge
    {
        /// <summary>
        /// The Lightrail transaction summary.
        /// </summary>
        public Transaction LightrailTransaction { get; set; }

        /// <summary>
        /// The Stripe transaction summary.
        /// </summary>
        public StripeCharge StripeCharge { get; set; }

        /// <summary>
        /// The amount Lightrail charged in the transaction.
        /// </summary>
        [JsonIgnore]
        public long LightrailAmount => LightrailTransaction?.Value ?? 0;

        /// <summary>
        /// The amount Stripe charged in the transaction.
        /// </summary>
        [JsonIgnore]
        public long StripeAmount => StripeCharge?.Amount ?? 0;

        /// <summary>
        /// The total amount charged in the transaction.
        /// </summary>
        [JsonIgnore]
        public long TotalAmount => LightrailAmount + StripeAmount;

        /// <summary>
        /// The currency charged in the transaction.
        /// </summary>
        [JsonIgnore]
        public string Currency => LightrailTransaction?.Currency ?? StripeCharge?.Currency;
    }
}
