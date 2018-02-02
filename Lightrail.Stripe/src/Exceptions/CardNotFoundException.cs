using System;

namespace Lightrail.Stripe.Exceptions
{
    [System.Serializable]
    public class CardNotFoundException : System.Exception
    {
        public CardNotFoundException() { }
        public CardNotFoundException(string message) : base(message) { }
        public CardNotFoundException(string message, System.Exception inner) : base(message, inner) { }
        protected CardNotFoundException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
