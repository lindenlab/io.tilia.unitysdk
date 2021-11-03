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
    public class TiliaResponse
    {
        // These values are preceeded by "Response" so that they
        // do not conflict with payload values on derived classes
        // which may have similar values, such as "Status"
        public string ResponseStatus;
        public List<string> ResponseMessages;
        public List<string> ResponseCodes;
        public int ResponseWebCode;

        // Only used if response is in a failed status.
        public Dictionary<string, string[]> ResponseErrors;

        public bool Recieved
        {
            get { return !String.IsNullOrEmpty(ResponseStatus); }
        }

        public bool Failed
        {
            get { return ResponseStatus == "Failed"; }
        }

        public bool Success
        {
            get { return ResponseStatus == "Success"; }
        }

        public TiliaResponse()
        {
            // Allow a blank to be created
            // This is needed by some children who can be instantiated
            // to use as input parameters into functions, such as TiliaInvoice.
        }

        public TiliaResponse(JToken import)
        {
            // Allow to be created with a direct payload injection.
            Import(import);
        }

        // Will be overridden by children classes to add unique payload processing,
        // but all returns share these bits in common. Always remember to Base.Import()
        // in overrides.
        public virtual void Import(JToken json)
        {
            if (!TiliaPay.TokenIsNull(json["web_response_code"]))
            {
                ResponseWebCode = json["web_response_code"].Value<int>();
            }

            ResponseStatus = TiliaPay.StringOrNull(json["status"]);
            ResponseMessages = new List<string>();
            if (!TiliaPay.TokenIsNull(json["message"]))
            {
                foreach (JToken msg in json["message"])
                {
                    ResponseMessages.Add(msg.ToString());
                }
            }
            ResponseCodes = new List<string>();
            if (!TiliaPay.TokenIsNull(json["codes"]))
            {
                foreach (JToken msg in json["codes"])
                {
                    ResponseCodes.Add(msg.ToString());
                }
            }
            if (Failed)
            {
                var payload = json["payload"];
                ResponseErrors = new Dictionary<string, string[]>();
                if (!TiliaPay.TokenIsNull(payload))
                {
                    if (payload.Type == JTokenType.String)
                    {
                        // Errors sometimes are strings.
                        if (String.IsNullOrEmpty(payload.ToString()))
                        {
                            ResponseErrors.Add("general", new string[] { payload.ToString() });
                        }
                    }
                    else if (!TiliaPay.TokenIsNull(payload["errors"]))
                    {
                        // Errors are sometimes JSON with string arrays.
                        foreach (var err in payload["errors"].Children<JProperty>())
                        {
                            var value = err.Value<JArray>();
                            string[] error_strings = new string[value.Count];
                            for (int e = 0; e < value.Count; e++)
                            {
                                error_strings[e] = value[e].ToString();
                            }
                            ResponseErrors.Add(err.Name, error_strings);
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    internal class TiliaToken : TiliaResponse
    {
        public string AccessToken;
        public string TokenType;
        public string Scope;
        public double ExpiresIn;
        public DateTime Created;

        public TiliaToken()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaToken(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        public bool IsValid()
        {
            return !String.IsNullOrEmpty(AccessToken) && Created.AddSeconds(ExpiresIn - 300) > DateTime.Now;
        }

        public override void Import(JToken json)
        {
            // Non-standard return format for tokens, don't do base import here from parent class.
            AccessToken = TiliaPay.StringOrNull(json["access_token"]);
            TokenType = TiliaPay.StringOrNull(json["token_type"]);
            ExpiresIn = json["expires_in"].Value<int>();
            Scope = TiliaPay.StringOrNull(json["scope"]);
            Created = DateTime.Now;
        }
    }

    [Serializable]
    public class TiliaInvoice : TiliaResponse
    {
        public string ID;
        public string AccountID;
        public string ReferenceType;
        public string ReferenceID;
        public string State;
        public string Description;
        public string MetaData;

        public List<TiliaSubItem> SubItems = new List<TiliaSubItem>();

        public TiliaInvoiceSummary Summary;
        public struct TiliaInvoiceSummary
        {
            public int TotalAmount;
            public string Currency;
            public string DisplayAmount;
        }

        public string FailureReason;
        public DateTime Created;
        public DateTime Updated;

        public List<TiliaLineItem> LineItems;

        public List<TiliaPaymentMethod> PaymentMethods;

        // This is a subclass because PaymentMethod is very different format
        // between TiliaInvoice and TiliaPaymentMethods.
        [Serializable]
        public class TiliaPaymentMethod
        {
            public string ID;
            public string Currency;
            public int Amount;
            public int AuthorizedAmount;
            public string DisplayAmount;

            public List<TiliaSubItem> SubItems = new List<TiliaSubItem>();

            public TiliaPaymentMethod()
            {
                // Nothing special here. Just has to be defined.
            }

            public TiliaPaymentMethod(JToken import)
            {
                Import(import);
            }

            public void Import(JToken import)
            {
                ID = TiliaPay.StringOrNull(import["payment_method_id"]);
                Currency = TiliaPay.StringOrNull(import["currency"]);
                DisplayAmount = TiliaPay.StringOrNull(import["display_amount"]);

                if (!TiliaPay.TokenIsNull(import["amount"]))
                {
                    Amount = import["amount"].Value<int>();
                }

                if (!TiliaPay.TokenIsNull(import["authorized_amount"]))
                {
                    AuthorizedAmount = import["authorized_amount"].Value<int>();
                }

                if (!TiliaPay.TokenIsNull(import["subitems"]))
                {
                    SubItems = new List<TiliaSubItem>();
                    foreach (JProperty item in import["subitems"])
                    {
                        SubItems.Add(new TiliaSubItem(item.Value));
                    }
                }
            }
        }

        public TiliaInvoice()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaInvoice(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            if (!Failed && !TiliaPay.TokenIsNull(payload))
            {
                ID = TiliaPay.StringOrNull(payload["invoice_id"]);
                AccountID = TiliaPay.StringOrNull(payload["account_id"]);
                ReferenceType = TiliaPay.StringOrNull(payload["reference_type"]);
                ReferenceID = TiliaPay.StringOrNull(payload["reference_id"]);
                State = TiliaPay.StringOrNull(payload["state"]);
                Description = TiliaPay.StringOrNull(payload["description"]);
                MetaData = TiliaPay.StringOrNull(payload["metadata"]);
                FailureReason = TiliaPay.StringOrNull(payload["failure_reason"]);

                if (!TiliaPay.TokenIsNull(payload["summary"]))
                {
                    Summary = new TiliaInvoiceSummary()
                    {
                        TotalAmount = payload["summary"]["total_amount"].Value<int>(),
                        Currency = TiliaPay.StringOrNull(payload["summary"]["currency"]),
                        DisplayAmount = TiliaPay.StringOrNull(payload["summary"]["display_amount"])
                    };
                }

                if (!TiliaPay.TokenIsNull(payload["created"]))
                {
                    try
                    {
                        Created = DateTime.Parse(payload["created"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }

                if (!TiliaPay.TokenIsNull(payload["updated"]))
                {
                    try
                    {
                        Updated = DateTime.Parse(payload["updated"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }

                if (!TiliaPay.TokenIsNull(payload["line_items"]))
                {
                    LineItems = new List<TiliaLineItem>();
                    foreach (JProperty item in payload["line_items"])
                    {
                        LineItems.Add(new TiliaLineItem(item.Value));
                    }
                }

                if (!TiliaPay.TokenIsNull(payload["payment_methods"]))
                {
                    PaymentMethods = new List<TiliaPaymentMethod>();
                    foreach (JProperty method in payload["payment_methods"])
                    {
                        PaymentMethods.Add(new TiliaPaymentMethod(method.Value));
                    }
                }

                if (!TiliaPay.TokenIsNull(payload["subitems"]))
                {
                    SubItems = new List<TiliaSubItem>();
                    foreach (JProperty item in payload["subitems"])
                    {
                        SubItems.Add(new TiliaSubItem(item.Value));
                    }
                }
            }
        }
    }

    [Serializable]
    public class TiliaLineItem
    {
        public string ID;
        public int Amount;
        public string Currency = "USD";
        public string TransactionType;
        public string ProductSKU;
        public string ReferenceType;
        public string ReferenceID;
        public string Description;
        public string MetaData;

        public List<TiliaSubItem> SubItems = new List<TiliaSubItem>();

        public List<TiliaRecipient> Recipients = new List<TiliaRecipient>();

        public TiliaLineItem()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaLineItem(JToken import)
        {
            Import(import);
        }

        public void Import(JToken import)
        {
            ID = TiliaPay.StringOrNull(import["line_item_id"]);
            Currency = TiliaPay.StringOrNull(import["currency"]);
            TransactionType = TiliaPay.StringOrNull(import["transaction_type"]);
            ProductSKU = TiliaPay.StringOrNull(import["product_sku"]);
            ReferenceType = TiliaPay.StringOrNull(import["reference_type"]);
            ReferenceID = TiliaPay.StringOrNull(import["reference_id"]);
            Description = TiliaPay.StringOrNull(import["description"]);
            MetaData = TiliaPay.StringOrNull(import["metadata"]);

            if (!TiliaPay.TokenIsNull(import["amount"]))
            {
                Amount = import["amount"].Value<int>();
            }

            if (!TiliaPay.TokenIsNull(import["subitems"]))
            {
                SubItems = new List<TiliaSubItem>();
                foreach (JProperty item in import["subitems"])
                {
                    SubItems.Add(new TiliaSubItem(item.Value));
                }
            }

            if (!TiliaPay.TokenIsNull(import["recipients"]))
            {
                Recipients = new List<TiliaRecipient>();
                foreach (JProperty item in import["recipients"])
                {
                    Recipients.Add(new TiliaRecipient(item.Value));
                }
            }
        }
    }

    [Serializable]
    public class TiliaRecipient
    {
        public int Amount;
        public string Currency = "USD";
        public bool IntegratorRevenue;
        public string ReferenceType;
        public string ReferenceID;
        public string Description;
        public string MetaData;
        public string SourceWalletID;
        public string DestinationWalletID;

        public TiliaRecipient()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaRecipient(JToken import)
        {
            Import(import);
        }

        public void Import(JToken import)
        {
            Currency = TiliaPay.StringOrNull(import["currency"]);
            ReferenceType = TiliaPay.StringOrNull(import["reference_type"]);
            ReferenceID = TiliaPay.StringOrNull(import["reference_id"]);
            Description = TiliaPay.StringOrNull(import["description"]);
            MetaData = TiliaPay.StringOrNull(import["metadata"]);
            SourceWalletID = TiliaPay.StringOrNull(import["source_wallet_id"]);
            DestinationWalletID = TiliaPay.StringOrNull(import["destination_wallet_id"]);

            if (!TiliaPay.TokenIsNull(import["integrator_revenue"]))
            {
                IntegratorRevenue = import["integrator_revenue"].ToObject<bool>();
            }

            if (!TiliaPay.TokenIsNull(import["amount"]))
            {
                Amount = import["amount"].Value<int>();
            }
        }
    }

    [Serializable]
    public class TiliaSubItem
    {
        public string ID;
        public int Amount;
        public string Currency;
        public string DisplayAmount;
        public string ReferenceType;
        public string ReferenceID;
        public string Description;
        public string MetaData;
        public string SourceAccountID;
        public string SourcePaymentMethodID;
        public string SourceWalletID;
        public string DestinationAccountID;
        public string DestinationPaymentMethodID;
        public string DestinationWalletID;

        public TiliaSubItem()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaSubItem(JToken import)
        {
            Import(import);
        }

        public void Import(JToken import)
        {
            ID = TiliaPay.StringOrNull(import["subitem_id"]);
            Currency = TiliaPay.StringOrNull(import["currency"]);
            DisplayAmount = TiliaPay.StringOrNull(import["display_amount"]);
            ReferenceType = TiliaPay.StringOrNull(import["reference_type"]);
            ReferenceID = TiliaPay.StringOrNull(import["reference_id"]);
            Description = TiliaPay.StringOrNull(import["description"]);
            MetaData = TiliaPay.StringOrNull(import["metadata"]);
            SourceAccountID = TiliaPay.StringOrNull(import["source_account_id"]);
            SourcePaymentMethodID = TiliaPay.StringOrNull(import["source_payment_method_id"]);
            SourceWalletID = TiliaPay.StringOrNull(import["source_wallet_id"]);
            DestinationAccountID = TiliaPay.StringOrNull(import["destination_account_id"]);
            DestinationPaymentMethodID = TiliaPay.StringOrNull(import["destination_payment_method_id"]);
            DestinationWalletID = TiliaPay.StringOrNull(import["destination_wallet_id"]);

            if (!TiliaPay.TokenIsNull(import["amount"]))
            {
                Amount = import["amount"].Value<int>();
            }
        }
    }

    [Serializable]
    public class TiliaRegistration : TiliaResponse
    {
        public string AccountID;
        public string AutologinID;
        public string MetaData;
        public string TrackingID;

        public TiliaRegistration()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaRegistration(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            if (!Failed && !TiliaPay.TokenIsNull(payload))
            {
                AccountID = TiliaPay.StringOrNull(payload["account_id"]);
                AutologinID = TiliaPay.StringOrNull(payload["autologin_id"]);
                MetaData = TiliaPay.StringOrNull(payload["metadata"]);
                TrackingID = TiliaPay.StringOrNull(payload["tracking_id"]);
            }
        }
    }

    [Serializable]
    public class TiliaUser : TiliaResponse
    {
        public string ID;
        public string UserName;
        public string Email;
        public string Integrator;
        public bool? IsBlocked;
        public DateTime? Created;

        public TiliaUser()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaUser(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            if (!Failed && !TiliaPay.TokenIsNull(payload))
            {
                ID = TiliaPay.StringOrNull(payload["account_id"]);
                UserName = TiliaPay.StringOrNull(payload["username"]);
                Email = TiliaPay.StringOrNull(payload["email"]);
                Integrator = TiliaPay.StringOrNull(payload["integrator"]);
                if (!TiliaPay.TokenIsNull(payload["is_blocked"]))
                {
                    IsBlocked = payload["is_blocked"].ToObject<bool>();
                }
                if (!TiliaPay.TokenIsNull(payload["created"]))
                {
                    try
                    {
                        Created = DateTime.Parse(payload["created"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }
            }
        }
    }

    [Serializable]
    public class TiliaKYC : TiliaResponse
    {
        public string ID;
        public string State;

        public TiliaKYC()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaKYC(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            if (!Failed && !TiliaPay.TokenIsNull(payload))
            {
                ID = TiliaPay.StringOrNull(payload["account_id"]);
                State = TiliaPay.StringOrNull(payload["state"]);
            }
        }
    }

    [Serializable]
    public class TiliaUserAuth : TiliaResponse
    {
        public string NonceAuthID;
        public string Redirect;

        public TiliaUserAuth()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaUserAuth(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            if (!Failed && !TiliaPay.TokenIsNull(payload))
            {
                NonceAuthID = TiliaPay.StringOrNull(payload["nonce_auth_id"]);
                Redirect = TiliaPay.StringOrNull(payload["redirect"]);
            }
        }
    }

    [Serializable]
    public class TiliaPaymentMethods : TiliaResponse
    {
        public List<TiliaPaymentMethod> PaymentMethods;

        // This is a subclass because PaymentMethod is very different format
        // between TiliaInvoice and TiliaPaymentMethods.
        [Serializable]
        public class TiliaPaymentMethod
        {
            public string ID;
            public string AccountID;
            public string MethodClass;
            public string DisplayString;
            public string Provider;
            public string PSPReference;
            public string PSPHashCode;
            public string ProcessingCurrency;
            public string PMState;
            public string Integrator;
            public DateTime? Created;
            public DateTime? Updated;

            // There's some wallet-specific variables we might encounter.
            public int WalletBalance;

            public TiliaPaymentMethod()
            {
                // Nothing special here. Just has to be defined.
            }

            public TiliaPaymentMethod(JToken import)
            {
                Import(import);
            }

            public void Import(JToken import)
            {
                ID = TiliaPay.StringOrNull(import["id"]);
                AccountID = TiliaPay.StringOrNull(import["account_id"]);
                MethodClass = TiliaPay.StringOrNull(import["method_class"]);
                DisplayString = TiliaPay.StringOrNull(import["display_string"]);
                Provider = TiliaPay.StringOrNull(import["provider"]);
                PSPReference = TiliaPay.StringOrNull(import["psp_reference"]);
                PSPHashCode = TiliaPay.StringOrNull(import["psp_hash_code"]);
                ProcessingCurrency = TiliaPay.StringOrNull(import["processing_currency"]);
                PMState = TiliaPay.StringOrNull(import["pm_state"]);
                Integrator = TiliaPay.StringOrNull(import["integrator"]);

                if (!TiliaPay.TokenIsNull(import["wallet_balance"]))
                {
                    WalletBalance = import["wallet_balance"].Value<int>();
                }

                if (!TiliaPay.TokenIsNull(import["created"]))
                {
                    try
                    {
                        Created = DateTime.Parse(import["created"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }

                if (!TiliaPay.TokenIsNull(import["updated"]))
                {
                    try
                    {
                        Updated = DateTime.Parse(import["updated"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }
            }

        }

        public TiliaPaymentMethods()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaPaymentMethods(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            PaymentMethods = new List<TiliaPaymentMethod>();
            if (!TiliaPay.TokenIsNull(payload))
            {
                foreach (var method in payload)
                {
                    PaymentMethods.Add(new TiliaPaymentMethod(method));
                }
            }
        }
    }

    [Serializable]
    public class TiliaEscrow : TiliaResponse
    {
        public string ID;
        public string AccountID;
        public string EscrowInvoiceID;
        public string CommitInvoiceID;
        public string CancelInvoiceID;
        public string Status;
        public string Integrator;
        public DateTime? Created;
        public DateTime? Updated;

        public TiliaEscrow()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaEscrow(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            if (!Failed && !TiliaPay.TokenIsNull(payload))
            {
                ID = TiliaPay.StringOrNull(payload["id"]);
                AccountID = TiliaPay.StringOrNull(payload["account_id"]);
                EscrowInvoiceID = TiliaPay.StringOrNull(payload["escrow_invoice_id"]);
                CommitInvoiceID = TiliaPay.StringOrNull(payload["commit_invoice_id"]);
                CancelInvoiceID = TiliaPay.StringOrNull(payload["cancel_invoice_id"]);
                Status = TiliaPay.StringOrNull(payload["status"]);
                Integrator = TiliaPay.StringOrNull(payload["integrator"]);
                if (!TiliaPay.TokenIsNull(payload["created"]))
                {
                    try
                    {
                        Created = DateTime.Parse(payload["created"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }

                if (!TiliaPay.TokenIsNull(payload["updated"]))
                {
                    try
                    {
                        Updated = DateTime.Parse(payload["updated"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }
            }
        }
    }

    [Serializable]
    public class TiliaPayout : TiliaResponse
    {
        public string ID;
        public string AccountID;
        public string CreditID;
        public string Status;
        public DateTime? Created;
        public DateTime? Updated;

        public TiliaCredit credit;

        [Serializable]
        public class TiliaCredit
        {
            public string DestinationPaymentMethodID;
            public int Amount;
            public int FeeAmount;
            public string Currency;
            public string Status;
        }

        public TiliaPayout()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaPayout(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            if (TiliaPay.TokenIsNull(payload))
            {
                // This might be an array of payouts, or it might be a standalone payout.
                // If it's an array of payouts, we're already inside the payload.
                payload = json;
            }
            if (!Failed && !TiliaPay.TokenIsNull(payload))
            {
                // ID can come from two possible sub values depending on whether
                // it was invoked from GetPayout or GetPayouts.
                if (!TiliaPay.TokenIsNull(payload["payout_id"]))
                {
                    ID = TiliaPay.StringOrNull(payload["payout_id"]);
                }
                else
                {
                    ID = TiliaPay.StringOrNull(payload["payout_status_id"]);
                }
                AccountID = TiliaPay.StringOrNull(payload["account_id"]);
                CreditID = TiliaPay.StringOrNull(payload["credit_id"]);
                Status = TiliaPay.StringOrNull(payload["status"]);
                if (!TiliaPay.TokenIsNull(payload["created"]))
                {
                    try
                    {
                        Created = DateTime.Parse(payload["created"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }

                if (!TiliaPay.TokenIsNull(payload["updated"]))
                {
                    try
                    {
                        Updated = DateTime.Parse(payload["updated"].ToString());
                    }
                    catch (FormatException)
                    {
                        // The date was messed up somehow. Huh.
                    }
                }

                if (!TiliaPay.TokenIsNull(payload["credit"]))
                {
                    credit = new TiliaCredit()
                    {
                        DestinationPaymentMethodID = TiliaPay.StringOrNull(payload["credit"]["destination_payment_method_id"]),
                        Currency = TiliaPay.StringOrNull(payload["credit"]["currency"]),
                        Status = TiliaPay.StringOrNull(payload["credit"]["status"]),
                        Amount = payload["credit"]["amount"].Value<int>(),
                        FeeAmount = !TiliaPay.TokenIsNull(payload["credit"]["fee_amount"]) ? payload["credit"]["fee_amount"].Value<int>() : 0
                    };
                }

            }
        }
    }

    [Serializable]
    public class TiliaPayouts : TiliaResponse
    {
        public List<TiliaPayout> Payouts;

        public TiliaPayouts()
        {
            // Nothing special here. Just has to be defined.
        }

        public TiliaPayouts(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            // Now let's do the special stuff.
            var payload = json["payload"];
            Payouts = new List<TiliaPayout>();
            if (!TiliaPay.TokenIsNull(payload))
            {
                foreach (var payout in payload)
                {
                    Payouts.Add(new TiliaPayout(payout));
                }
            }
        }
    }

}
