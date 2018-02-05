using Lightrail;
using Lightrail.Model;
using Lightrail.Params;
using Lightrail.Stripe;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stripe;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lightrail.Stripe.Test
{
    [TestClass]
    public class StripeLightrailSplitTenderServiceTest
    {
        private LightrailClient _lightrail;
        private StripeLightrailSplitTenderService _service;

        [TestInitialize]
        public void Before()
        {
            DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".env"));
            StripeConfiguration.SetApiKey(Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY"));
            _lightrail = new LightrailClient
            {
                ApiKey = Environment.GetEnvironmentVariable("LIGHTRAIL_API_KEY")
            };
            _service = new StripeLightrailSplitTenderService(_lightrail);
        }

        [TestMethod]
        public async Task TestSimulateAndCreate()
        {
            var shopperId = Guid.NewGuid().ToString();
            var card = await _lightrail.Accounts.CreateAccount(
                new ContactIdentifier { ShopperId = shopperId },
                new CreateAccountCardParams
                {
                    UserSuppliedId = Guid.NewGuid().ToString(),
                    Currency = "CAD",
                    InitialValue = 8462
                });
            Assert.IsNotNull(card);
            Assert.AreEqual("CAD", card.Currency);

            var simulateResult = await _service.Simulate(new StripeLightrailSplitTenderSimulateOptions
            {
                UserSuppliedId = Guid.NewGuid().ToString(),
                Currency = "CAD",
                Amount = 9999,
                LightrailShare = 9999,
                ShopperId = shopperId,
                Nsf = false
            });
            Assert.IsNotNull(simulateResult);
            Assert.IsNotNull(simulateResult.LightrailTransaction);
            Assert.AreEqual(-8462, simulateResult.LightrailAmount);
            Assert.AreEqual(-8462, simulateResult.LightrailTransaction.Value);
            Assert.IsNull(simulateResult.StripeCharge);
            Assert.AreEqual(0, simulateResult.StripeAmount);

            var lightrailOnlyResult = await _service.Create(new StripeLightrailSplitTenderCreateOptions
            {
                UserSuppliedId = Guid.NewGuid().ToString(),
                Currency = "CAD",
                Amount = 40,
                LightrailShare = 40,
                ShopperId = shopperId
            });
            Assert.IsNotNull(lightrailOnlyResult);
            Assert.IsNotNull(lightrailOnlyResult.LightrailTransaction);
            Assert.AreEqual(-40, lightrailOnlyResult.LightrailAmount);
            Assert.AreEqual(-40, lightrailOnlyResult.LightrailTransaction.Value);
            Assert.IsNull(lightrailOnlyResult.StripeCharge);
            Assert.AreEqual(0, lightrailOnlyResult.StripeAmount);

            var splitChargeResult = await _service.Create(new StripeLightrailSplitTenderCreateOptions
            {
                UserSuppliedId = Guid.NewGuid().ToString(),
                Currency = "CAD",
                Amount = 9999,
                LightrailShare = 8422,
                ShopperId = shopperId,
                Source = "tok_visa"
            });
            Assert.IsNotNull(splitChargeResult);
            Assert.IsNotNull(splitChargeResult.LightrailTransaction);
            Assert.AreEqual(-8422, splitChargeResult.LightrailAmount);
            Assert.AreEqual(-8422, splitChargeResult.LightrailTransaction.Value);
            Assert.IsNotNull(splitChargeResult.StripeCharge);
            Assert.AreEqual(1577, splitChargeResult.StripeAmount);
            Assert.AreEqual(1577, splitChargeResult.StripeCharge.Amount);

            var stripeOnlyResult = await _service.Create(new StripeLightrailSplitTenderCreateOptions
            {
                UserSuppliedId = Guid.NewGuid().ToString(),
                Currency = "CAD",
                Amount = 1226,
                LightrailShare = 0,
                ShopperId = shopperId,
                Source = "tok_visa"
            });
            Assert.IsNotNull(stripeOnlyResult);
            Assert.IsNull(stripeOnlyResult.LightrailTransaction);
            Assert.AreEqual(0, stripeOnlyResult.LightrailAmount);
            Assert.IsNotNull(stripeOnlyResult.StripeCharge);
            Assert.AreEqual(1226, stripeOnlyResult.StripeAmount);
            Assert.AreEqual(1226, stripeOnlyResult.StripeCharge.Amount);
        }
    }
}
