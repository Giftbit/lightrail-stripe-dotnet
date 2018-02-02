using Lightrail;
using Lightrail.Model;
using Lightrail.Params;
using Lightrail.Stripe.Exceptions;
using Newtonsoft.Json.Linq;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lightrail.Stripe
{
    public class StripeLightrailSplitTenderService
    {
        private LightrailClient _lightrail;

        public StripeLightrailSplitTenderService(LightrailClient lightrail)
        {
            if (lightrail == null)
            {
                throw new ArgumentNullException(nameof(lightrail));
            }
            _lightrail = lightrail;
        }

        public async Task<StripeLightrailSplitTenderCharge> Simulate(StripeLightrailSplitTenderSimulateOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Currency == null)
            {
                throw new ArgumentException("Currency is required");
            }
            if (options.ShopperId == null)
            {
                throw new ArgumentException("ShopperId is required");
            }
            if (options.Amount <= 0)
            {
                throw new ArgumentException("Amount must be > 0");
            }
            if (options.LightrailShare < 0)
            {
                throw new ArgumentException("LightrailShare must be >= 0");
            }
            if (options.LightrailShare > options.Amount)
            {
                throw new ArgumentException("LightrailShare must be <= Amount");
            }
            options.EnsureUserSuppliedId();

            var splitTenderSimulation = new StripeLightrailSplitTenderCharge();
            if (options.LightrailShare > 0)
            {
                var card = await _lightrail.Accounts.GetAccount(new ContactIdentifier { ShopperId = options.ShopperId }, options.Currency);
                if (card == null)
                {
                    throw new CardNotFoundException($"No {options.Currency} card found for shopperId '{options.ShopperId}'.");
                }

                var lightrailTransactionParams = new SimulateTransactionParams
                {
                    Value = 0 - options.LightrailShare,
                    Currency = options.Currency,
                    UserSuppliedId = options.UserSuppliedId,
                    Nsf = options.Nsf
                };
                lightrailTransactionParams.Metadata = MergeDictionaries(options.Metadata, GetAdditionalLightrailMetadata(options.Amount));
                splitTenderSimulation.LightrailTransaction = await _lightrail.Cards.Transactions.SimulateTransaction(card, lightrailTransactionParams);
            }

            return splitTenderSimulation;
        }

        public async Task<StripeLightrailSplitTenderCharge> Create(StripeLightrailSplitTenderCreateOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Currency == null)
            {
                throw new ArgumentException("Currency is required");
            }
            if (options.ShopperId == null)
            {
                throw new ArgumentException("ShopperId is required");
            }
            if (options.Amount <= 0)
            {
                throw new ArgumentException("Amount must be > 0");
            }
            if (options.LightrailShare < 0)
            {
                throw new ArgumentException("LightrailShare must be >= 0");
            }
            if (options.LightrailShare > options.Amount)
            {
                throw new ArgumentException("LightrailShare must be <= Amount");
            }
            options.EnsureUserSuppliedId();

            var splitTenderCharge = new StripeLightrailSplitTenderCharge();
            if (options.LightrailShare > 0)
            {
                var card = await _lightrail.Accounts.GetAccount(new ContactIdentifier { ShopperId = options.ShopperId }, options.Currency);
                if (card == null)
                {
                    throw new CardNotFoundException($"No {options.Currency} card found for shopperId '{options.ShopperId}'.");
                }

                var lightrailTransactionParams = new CreateTransactionParams
                {
                    Value = 0 - options.LightrailShare,
                    Currency = options.Currency,
                    UserSuppliedId = options.UserSuppliedId,
                    Pending = options.LightrailShare < options.Amount
                };
                lightrailTransactionParams.Metadata = MergeDictionaries(options.Metadata, GetAdditionalLightrailMetadata(options.Amount));
                var lightrailPendingTransaction = await _lightrail.Cards.Transactions.CreateTransaction(card, lightrailTransactionParams);
                splitTenderCharge.LightrailTransaction = lightrailPendingTransaction;

                if (options.LightrailShare < options.Amount)
                {
                    try
                    {
                        splitTenderCharge.StripeCharge = await CreateStripeCharge(options, null);

                        var captureParams = new CapturePendingTransactionParams
                        {
                            UserSuppliedId = options.UserSuppliedId + "-capture",
                            Metadata = MergeDictionaries(options.Metadata, GetAdditionalLightrailMetadata(options.Amount, splitTenderCharge.StripeCharge))
                        };
                        splitTenderCharge.LightrailTransaction = await _lightrail.Cards.Transactions.CapturePending(card, lightrailPendingTransaction, captureParams);
                    }
                    catch (Exception)
                    {
                        var voidParams = new VoidPendingTransactionParams
                        {
                            UserSuppliedId = options.UserSuppliedId + "-void",
                            Metadata = MergeDictionaries(options.Metadata, GetAdditionalLightrailMetadata(options.Amount, splitTenderCharge.StripeCharge))
                        };
                        splitTenderCharge.LightrailTransaction = await _lightrail.Cards.Transactions.VoidPending(card, splitTenderCharge.LightrailTransaction, voidParams);
                        throw;
                    }
                }
            }
            else
            {
                splitTenderCharge.StripeCharge = await CreateStripeCharge(options, null);
            }

            return splitTenderCharge;
        }

        private Task<StripeCharge> CreateStripeCharge(StripeLightrailSplitTenderCreateOptions options, Transaction lightrailTransaction)
        {
            var stripeCreateChargeOptions = new StripeChargeCreateOptions
            {
                Amount = (int)(options.Amount - options.LightrailShare),
                Currency = options.Currency,
                SourceTokenOrExistingSourceId = options.Source,
                Metadata = JObject.FromObject(options.Metadata ?? new Dictionary<string, object>()).ToObject<Dictionary<string, string>>()
            };

            stripeCreateChargeOptions.Metadata.Add("_split_tender_total", options.Amount.ToString());
            stripeCreateChargeOptions.Metadata.Add("_split_tender_partner", "LIGHTRAIL");
            stripeCreateChargeOptions.Metadata.Add("_split_tender_partner_transaction_id", lightrailTransaction?.TransactionId ?? "");

            var stripeRequestOptions = new StripeRequestOptions {
                IdempotencyKey = options.UserSuppliedId
            };

            var stripeChargeService = new StripeChargeService();
            return stripeChargeService.CreateAsync(stripeCreateChargeOptions, stripeRequestOptions);
        }

        private IDictionary<string, object> GetAdditionalLightrailMetadata(long amount, StripeCharge stripeCharge = null)
        {
            var metadata = new Dictionary<string, object>
            {
                {"_split_tender_total", amount},
                {"_split_tender_partner", "STRIPE"}
            };
            if (stripeCharge != null)
            {
                metadata.Add("_split_tender_partner_transaction_id", stripeCharge.Id);
            }
            return metadata;
        }

        private IDictionary<T, K> MergeDictionaries<T, K>(IDictionary<T, K> first, IDictionary<T, K> second)
        {
            var merged = new Dictionary<T, K>();
            if (first != null)
            {
                first.ToList().ForEach(kvp => merged[kvp.Key] = kvp.Value);
            }
            if (second != null)
            {
                second.ToList().ForEach(kvp => merged[kvp.Key] = kvp.Value);
            }
            return merged;
        }
    }
}
