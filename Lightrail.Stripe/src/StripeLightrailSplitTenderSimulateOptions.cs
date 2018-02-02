using Lightrail;
using Lightrail.Params;
using System;
using System.Collections.Generic;

namespace Lightrail.Stripe
{
    public class StripeLightrailSplitTenderSimulateOptions : IUserSuppliedIdRequired
    {
        public string UserSuppliedId { get; set; }
        public string Currency { get; set; }
        public long Amount { get; set; }
        public long LightrailShare { get; set; }
        public string ShopperId { get; set; }
        public string Source { get; set; }
        public string Customer { get; set; }
        public IDictionary<string, object> Metadata { get; set; }
        public bool? Nsf { get; set; }
    }
}
