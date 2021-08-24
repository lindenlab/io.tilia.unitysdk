using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tilia.Pay
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

        // Capture for debug purposes.
        //public JObject raw_json;

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
            // Capture for debug purposes.
            //raw_json = json;

            if (!Tilia.TokenIsNull(json["web_response_code"]))
            {
                ResponseWebCode = Int32.Parse(json["web_response_code"].ToString());
            }

            ResponseStatus = Tilia.StringOrNull(json["status"]);
            ResponseMessages = new List<string>();
            if (!Tilia.TokenIsNull(json["message"]))
            {
                foreach (JToken msg in json["message"])
                {
                    ResponseMessages.Add(msg.ToString());
                }
            }
            ResponseCodes = new List<string>();
            if (!Tilia.TokenIsNull(json["codes"]))
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
                if (!Tilia.TokenIsNull(payload))
                {
                    if (payload.Type == JTokenType.String)
                    {
                        // Errors sometimes are strings.
                        if (String.IsNullOrEmpty(payload.ToString()))
                        {
                            ResponseErrors.Add("general", new string[] { payload.ToString() });
                        }
                    }
                    else if (!Tilia.TokenIsNull(payload["errors"]))
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
            // Non-standard return format for tokens, don't do base.
            //base.Import(json);
            AccessToken = Tilia.StringOrNull(json["access_token"]);
            TokenType = Tilia.StringOrNull(json["token_type"]);
            ExpiresIn = Int32.Parse(json["expires_in"].ToString());
            Scope = Tilia.StringOrNull(json["scope"]);
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

        public List<TiliaSubItem> SubItems;

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

        // Maybe Invoice SubItems here. Might be vestigial.

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

            public List<TiliaSubItem> SubItems;

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
                ID = Tilia.StringOrNull(import["payment_method_id"]);
                Currency = Tilia.StringOrNull(import["currency"]);
                DisplayAmount = Tilia.StringOrNull(import["display_amount"]);

                if (!Tilia.TokenIsNull(import["amount"]))
                {
                    Amount = Int32.Parse(import["amount"].ToString());
                }

                if (!Tilia.TokenIsNull(import["authorized_amount"]))
                {
                    AuthorizedAmount = Int32.Parse(import["authorized_amount"].ToString());
                }

                if (!Tilia.TokenIsNull(import["SubItems"]))
                {
                    SubItems = new List<TiliaSubItem>();
                    foreach (var item in import["SubItems"])
                    {
                        SubItems.Add(new TiliaSubItem(item));
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
            if (!Failed && !Tilia.TokenIsNull(payload))
            {
                ID = Tilia.StringOrNull(payload["invoice_id"]);
                AccountID = Tilia.StringOrNull(payload["account_id"]);
                ReferenceType = Tilia.StringOrNull(payload["reference_type"]);
                ReferenceID = Tilia.StringOrNull(payload["reference_id"]);
                State = Tilia.StringOrNull(payload["state"]);
                Description = Tilia.StringOrNull(payload["description"]);
                MetaData = Tilia.StringOrNull(payload["metadata"]);
                FailureReason = Tilia.StringOrNull(payload["failure_reason"]);

                if (!Tilia.TokenIsNull(payload["summary"]))
                {
                    Summary = new TiliaInvoiceSummary()
                    {
                        TotalAmount = Int32.Parse(payload["total_amount"].ToString()),
                        Currency = Tilia.StringOrNull(payload["currency"]),
                        DisplayAmount = Tilia.StringOrNull(payload["display_amount"])
                    };
                }

                if (!Tilia.TokenIsNull(payload["created"]))
                {
                    Created = DateTime.Parse(payload["created"].ToString());
                }

                if (!Tilia.TokenIsNull(payload["updated"]))
                {
                    Updated = DateTime.Parse(payload["updated"].ToString());
                }

                if (!Tilia.TokenIsNull(payload["LineItems"]))
                {
                    LineItems = new List<TiliaLineItem>();
                    foreach (var item in payload["LineItems"])
                    {
                        LineItems.Add(new TiliaLineItem(item));
                    }
                }

                if (!Tilia.TokenIsNull(payload["PaymentMethods"]))
                {
                    PaymentMethods = new List<TiliaPaymentMethod>();
                    foreach (var method in payload["PaymentMethods"])
                    {
                        PaymentMethods.Add(new TiliaPaymentMethod(method));
                    }
                }

                if (!Tilia.TokenIsNull(payload["SubItems"]))
                {
                    SubItems = new List<TiliaSubItem>();
                    foreach (var item in payload["SubItems"])
                    {
                        SubItems.Add(new TiliaSubItem(item));
                    }
                }
            }
        }

        internal JObject Export()
        {
            var paymentMethods = new JArray();
            foreach (var method in PaymentMethods)
            {
                paymentMethods.Add(new JObject(
                    new JProperty("payment_method_id", method.ID),
                    new JProperty("amount", method.Amount)
                ));
            }
            var lineItems = new JArray();
            foreach (var line in LineItems)
            {
                lineItems.Add(new JObject(
                    new JProperty("amount", line.Amount),
                    new JProperty("currency", line.Currency),
                    new JProperty("description", line.Description),
                    new JProperty("metadata", line.MetaData),
                    new JProperty("product_sku", line.ProductSKU),
                    new JProperty("transaction_type", line.TransactionType),
                    new JProperty("reference_type", line.ReferenceType),
                    new JProperty("reference_id", line.ReferenceID)
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

        public List<TiliaSubItem> SubItems;

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
            ID = Tilia.StringOrNull(import["line_item_id"]);
            Currency = Tilia.StringOrNull(import["currency"]);
            TransactionType = Tilia.StringOrNull(import["transaction_type"]);
            ProductSKU = Tilia.StringOrNull(import["product_sku"]);
            ReferenceType = Tilia.StringOrNull(import["reference_type"]);
            ReferenceID = Tilia.StringOrNull(import["reference_id"]);
            Description = Tilia.StringOrNull(import["description"]);
            MetaData = Tilia.StringOrNull(import["metadata"]);

            if (!Tilia.TokenIsNull(import["amount"]))
            {
                Amount = Int32.Parse(import["amount"].ToString());
            }

            if (!Tilia.TokenIsNull(import["SubItems"]))
            {
                SubItems = new List<TiliaSubItem>();
                foreach (var item in import["SubItems"])
                {
                    SubItems.Add(new TiliaSubItem(item));
                }
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
            ID = Tilia.StringOrNull(import["subitem_id"]);
            Currency = Tilia.StringOrNull(import["currency"]);
            DisplayAmount = Tilia.StringOrNull(import["display_amount"]);
            ReferenceType = Tilia.StringOrNull(import["reference_type"]);
            ReferenceID = Tilia.StringOrNull(import["reference_id"]);
            Description = Tilia.StringOrNull(import["description"]);
            MetaData = Tilia.StringOrNull(import["metadata"]);
            SourceAccountID = Tilia.StringOrNull(import["source_account_id"]);
            SourcePaymentMethodID = Tilia.StringOrNull(import["source_payment_method_id"]);
            SourceWalletID = Tilia.StringOrNull(import["source_wallet_id"]);
            DestinationAccountID = Tilia.StringOrNull(import["destination_account_id"]);
            DestinationPaymentMethodID = Tilia.StringOrNull(import["destination_payment_method_id"]);
            DestinationWalletID = Tilia.StringOrNull(import["destination_wallet_id"]);

            if (!Tilia.TokenIsNull(import["amount"]))
            {
                Amount = Int32.Parse(import["amount"].ToString());
            }
        }
    }

    [Serializable]
    public class TiliaNewUser
    {
        public string UserName;
        public string Password;
        public string Email;
        public bool? TOS;
        public string MetaData;
        public string TrackingID;

        internal JObject Export()
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
    public class TiliaRegistration : TiliaResponse
    {
        public string AccountID;
        public string AutologinID;
        public string MetaData;
        public string TrackingID;
        public bool? AccountAlreadyExists;

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
            if (!Failed && !Tilia.TokenIsNull(payload))
            {
                AccountID = Tilia.StringOrNull(payload["account_id"]);
                AutologinID = Tilia.StringOrNull(payload["autologin_id"]);
                MetaData = Tilia.StringOrNull(payload["metadata"]);
                TrackingID = Tilia.StringOrNull(payload["tracking_id"]);
                if (!Tilia.TokenIsNull(payload["account_already_exists"]))
                {
                    AccountAlreadyExists = payload["account_already_exists"].ToObject<bool>();
                }
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
            if (!Failed && !Tilia.TokenIsNull(payload))
            {
                ID = Tilia.StringOrNull(payload["account_id"]);
                UserName = Tilia.StringOrNull(payload["username"]);
                Email = Tilia.StringOrNull(payload["email"]);
                Integrator = Tilia.StringOrNull(payload["integrator"]);
                if (!Tilia.TokenIsNull(payload["is_blocked"]))
                {
                    IsBlocked = payload["is_blocked"].ToObject<bool>();
                }
                if (!Tilia.TokenIsNull(payload["created"]))
                {
                    Created = DateTime.Parse(payload["created"].ToString());
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
            if (!Failed && !Tilia.TokenIsNull(payload))
            {
                ID = Tilia.StringOrNull(payload["account_id"]);
                State = Tilia.StringOrNull(payload["state"]);
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
            if (!Failed && !Tilia.TokenIsNull(payload))
            {
                NonceAuthID = Tilia.StringOrNull(payload["nonce_auth_id"]);
                Redirect = Tilia.StringOrNull(payload["redirect"]);
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
            public string PaymentMethodID;
            public string FirstName;
            public string LastName;
            public string FullName;
            public string Expiration;
            public string Address1;
            public string Address2;
            public string City;
            public string State;
            public string CountryISO;
            public string GeoIPState;
            public string GeoIPCountryISO;
            public string ZIP;
            public string BIN;
            public string LastFour;
            public string AVS;

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
                ID = Tilia.StringOrNull(import["id"]);
                AccountID = Tilia.StringOrNull(import["account_id"]);
                MethodClass = Tilia.StringOrNull(import["method_class"]);
                DisplayString = Tilia.StringOrNull(import["display_string"]);
                Provider = Tilia.StringOrNull(import["provider"]);
                PSPReference = Tilia.StringOrNull(import["psp_reference"]);
                PSPHashCode = Tilia.StringOrNull(import["psp_hash_code"]);
                ProcessingCurrency = Tilia.StringOrNull(import["processing_currency"]);
                PMState = Tilia.StringOrNull(import["pm_state"]);
                Integrator = Tilia.StringOrNull(import["integrator"]);
                PaymentMethodID = Tilia.StringOrNull(import["payment_method_id"]);
                FirstName = Tilia.StringOrNull(import["first_name"]);
                LastName = Tilia.StringOrNull(import["last_name"]);
                FullName = Tilia.StringOrNull(import["full_name"]);
                Expiration = Tilia.StringOrNull(import["expiration"]);
                Address1 = Tilia.StringOrNull(import["address1"]);
                Address2 = Tilia.StringOrNull(import["address2"]);
                City = Tilia.StringOrNull(import["city"]);
                State = Tilia.StringOrNull(import["state"]);
                CountryISO = Tilia.StringOrNull(import["country_iso"]);
                GeoIPState = Tilia.StringOrNull(import["geoip_state"]);
                GeoIPCountryISO = Tilia.StringOrNull(import["geoip_country_iso"]);
                ZIP = Tilia.StringOrNull(import["zip"]);
                BIN = Tilia.StringOrNull(import["bin"]);
                LastFour = Tilia.StringOrNull(import["last_four"]);
                AVS = Tilia.StringOrNull(import["avs"]);

                if (!Tilia.TokenIsNull(import["wallet_balance"]))
                {
                    WalletBalance = Int32.Parse(import["wallet_balance"].ToString());
                }

                if (!Tilia.TokenIsNull(import["created"]))
                {
                    Created = DateTime.Parse(import["created"].ToString());
                }

                if (!Tilia.TokenIsNull(import["updated"]))
                {
                    Updated = DateTime.Parse(import["updated"].ToString());
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
            if (!Tilia.TokenIsNull(payload))
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
            if (!Failed && !Tilia.TokenIsNull(payload))
            {
                ID = Tilia.StringOrNull(payload["id"]);
                AccountID = Tilia.StringOrNull(payload["account_id"]);
                EscrowInvoiceID = Tilia.StringOrNull(payload["escrow_invoice_id"]);
                CommitInvoiceID = Tilia.StringOrNull(payload["commit_invoice_id"]);
                CancelInvoiceID = Tilia.StringOrNull(payload["cancel_invoice_id"]);
                Status = Tilia.StringOrNull(payload["status"]);
                Integrator = Tilia.StringOrNull(payload["integrator"]);
                if (!Tilia.TokenIsNull(payload["created"]))
                {
                    Created = DateTime.Parse(payload["created"].ToString());
                }

                if (!Tilia.TokenIsNull(payload["updated"]))
                {
                    Updated = DateTime.Parse(payload["updated"].ToString());
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
            if (Tilia.TokenIsNull(payload))
            {
                // This might be an array of payouts, or it might be a standalone payout.
                // If it's an array of payouts, we're already inside the payload.
                payload = json;
            }
            if (!Failed && !Tilia.TokenIsNull(payload))
            {
                // ID can come from two possible sub values depending on whether
                // it was invoked from GetPayout or GetPayouts.
                if (!Tilia.TokenIsNull(payload["payout_id"]))
                {
                    ID = Tilia.StringOrNull(payload["payout_id"]);
                }
                else
                {
                    ID = Tilia.StringOrNull(payload["payout_status_id"]);
                }
                AccountID = Tilia.StringOrNull(payload["account_id"]);
                CreditID = Tilia.StringOrNull(payload["credit_id"]);
                Status = Tilia.StringOrNull(payload["status"]);
                if (!Tilia.TokenIsNull(payload["created"]))
                {
                    Created = DateTime.Parse(payload["created"].ToString());
                }

                if (!Tilia.TokenIsNull(payload["updated"]))
                {
                    Updated = DateTime.Parse(payload["updated"].ToString());
                }

                if (!Tilia.TokenIsNull(payload["credit"]))
                {
                    credit = new TiliaCredit()
                    {
                        DestinationPaymentMethodID = Tilia.StringOrNull(payload["credit"]["destination_payment_method_id"]),
                        Currency = Tilia.StringOrNull(payload["credit"]["currency"]),
                        Status = Tilia.StringOrNull(payload["credit"]["status"]),
                        Amount = Int32.Parse(payload["credit"]["amount"].ToString()),
                        FeeAmount = !Tilia.TokenIsNull(payload["credit"]["fee_amount"]) ? Int32.Parse(payload["credit"]["fee_amount"].ToString()) : 0
                    };
                }

            }
        }
    }

    [Serializable]
    public class TiliaNewPayout : TiliaResponse
    {
        public string SourcePaymentMethodID;
        public string DestinationPaymentMethodID;
        public int Amount;
        public string Currency = "USD";

        internal JObject Export()
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
            if (!Tilia.TokenIsNull(payload))
            {
                foreach (var payout in payload)
                {
                    Payouts.Add(new TiliaPayout(payout));
                }
            }
        }
    }

}
