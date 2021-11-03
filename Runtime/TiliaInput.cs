using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tilia
{
    [Serializable]
    public abstract class TiliaInput
    {
        internal abstract JObject Export();
    }

    [Serializable]
    public class TiliaNewUser : TiliaInput
    {
        public string UserName;
        public string Password;
        public string Email;
        public bool? TOS;
        public string MetaData;
        public string TrackingID;

        internal override JObject Export()
        {
            var jsonData = new JObject(
                new JProperty("username", UserName),
                new JProperty("password", Password),
                new JProperty("email", Email)
            );
            if (TOS != null)
            {
                jsonData.Add(new JProperty("tos", TOS));
            }
            if (!String.IsNullOrEmpty(MetaData))
            {
                jsonData.Add(new JProperty("metadata", MetaData));
            }
            if (!String.IsNullOrEmpty(TrackingID))
            {
                jsonData.Add(new JProperty("tracking_id", TrackingID));
            }
            return jsonData;
        }
    }

    [Serializable]
    public class TiliaPayment : TiliaInput
    {
        public string ID;
        public int Amount = 0;

        internal override JObject Export()
        {
            return new JObject(
                new JProperty("payment_method_id", ID),
                new JProperty("amount", Amount)
            );
        }
    }

    [Serializable]
    public class TiliaNewInvoice : TiliaInput
    {
        public string ID;
        public string AccountID;
        public string ReferenceType;
        public string ReferenceID;
        public string State;
        public string Description;
        public string MetaData;

        public List<TiliaLineItem> LineItems = new List<TiliaLineItem>();

        public List<TiliaPayment> PaymentMethods = new List<TiliaPayment>();

        internal override JObject Export()
        {
            var paymentMethods = new JArray();
            foreach (var method in PaymentMethods)
            {
                paymentMethods.Add(method.Export());
            }
            var lineItems = new JArray();
            foreach (var line in LineItems)
            {
                var recipients = new JArray();
                if (line.Recipients != null && line.Recipients.Count > 0)
                {
                    foreach (var recipient in line.Recipients)
                    {
                        recipients.Add(new JObject(
                            new JProperty("amount", recipient.Amount),
                            new JProperty("description", recipient.Description),
                            new JProperty("metadata", recipient.MetaData),
                            new JProperty("currency", recipient.Currency),
                            new JProperty("source_wallet_id", recipient.SourceWalletID),
                            new JProperty("destination_wallet_id", recipient.DestinationWalletID),
                            new JProperty("integrator_revenue", recipient.IntegratorRevenue),
                            new JProperty("reference_type", recipient.ReferenceType),
                            new JProperty("reference_id", recipient.ReferenceID)
                        ));
                    }
                }
                lineItems.Add(new JObject(
                    new JProperty("amount", line.Amount),
                    new JProperty("currency", line.Currency),
                    new JProperty("description", line.Description),
                    new JProperty("metadata", line.MetaData),
                    new JProperty("product_sku", line.ProductSKU),
                    new JProperty("transaction_type", line.TransactionType),
                    new JProperty("reference_type", line.ReferenceType),
                    new JProperty("reference_id", line.ReferenceID),
                    new JProperty("recipients", recipients)
                ));
            }
            var jsonData = new JObject(
                new JProperty("account_id", AccountID),
                new JProperty("reference_type", ReferenceType),
                new JProperty("reference_id", ReferenceID),
                new JProperty("description", Description),
                new JProperty("metadata", MetaData),
                new JProperty("payment_methods", paymentMethods),
                new JProperty("line_items", lineItems)
            );
            return jsonData;
        }
    }

    [Serializable]
    public class TiliaNewPayout : TiliaInput
    {
        public string SourcePaymentMethodID;
        public string DestinationPaymentMethodID;
        public int Amount;
        public string Currency = "USD";

        internal override JObject Export()
        {
            var jsonData = new JObject(
                new JProperty("source_payment_method_id", SourcePaymentMethodID),
                new JProperty("destination_payment_method_id", DestinationPaymentMethodID),
                new JProperty("amount", Amount),
                new JProperty("currency", Currency)
            );
            return jsonData;
        }
    }

}
