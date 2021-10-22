using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System;
using UnityEngine;
using UnityEngine.UI;
using Tilia;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TiliaTest : MonoBehaviour
{

    public TiliaPay TiliaPaySDK;

    public InputField UserNameInput;
    public InputField PasswordInput;
    public InputField EmailInput;
    public InputField AccountInput;

    public InputField PurchasePrice;
    public InputField PurchaseProduct;
    public InputField PurchaseAccount;
    public InputField PurchasePSP;
    public InputField PurchaseInvoice;

    public InputField RedirectURLInput;

    public InputField PayoutDestinationID;
    public InputField PayoutSourceID;
    public InputField PayoutAmount;
    public InputField PayoutAccount;
    public InputField PayoutStatusID;

    public void Awake()
    {
        if (TiliaPaySDK == null || !TiliaPaySDK.gameObject.activeInHierarchy)
        {
            // Don't include in active
            Debug.Log("TiliaPaySDK missing or inactive, looking for active SDK.");
            TiliaPaySDK = FindObjectOfType<TiliaPay>();
            if (TiliaPaySDK == null)
            {
                Debug.Log("Failed to find active TiliaPay component in scene. This won't work.");
            }
            else
            {
                Debug.Log("We found an active TiliaPay component on " + TiliaPaySDK.gameObject.name + ".");
            }
        }
    }

    public void CreatePayout()
    {
        var price = Int32.Parse(PayoutAmount.text, CultureInfo.InvariantCulture);
        var destID = PayoutDestinationID.text;
        var sourceID = PayoutSourceID.text;
        var accountID = PayoutAccount.text;
        Debug.Log("Attempting to create a payout for " + accountID + " from " + sourceID + " to " + destID + " for " + price + " cents USD):");
        var newPayout = new TiliaNewPayout()
        {
            DestinationPaymentMethodID = destID,
            SourcePaymentMethodID = sourceID,
            Amount = price,
            Currency = "USD"
        };
        TiliaPaySDK.CreatePayout(accountID, newPayout,
            (value) =>
            {
                if (value.Success)
                {
                    PayoutStatusID.text = value.ID;
                    Debug.Log("Payout created.");
                }
                else
                {
                    Debug.Log("Something went wrong creating the payout.");
                }
            });
    }

    public void CopyAccountToPayout()
    {
        PayoutAccount.text = AccountInput.text;
    }

    public void GetPayout()
    {
        var payoutID = PayoutStatusID.text;
        var accountID = PayoutAccount.text;
        TiliaPaySDK.GetPayout(accountID, payoutID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Payout found.");
                    Debug.Log(JsonConvert.SerializeObject(value, Formatting.Indented));
                }
                else
                {
                    Debug.Log("Something went wrong getting the payout.");
                }
            });
    }

    public void GetAllPayouts()
    {
        var accountID = PayoutAccount.text;
        TiliaPaySDK.GetPayouts(accountID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Payout retrieved.");
                    Debug.Log(JsonConvert.SerializeObject(value, Formatting.Indented));
                }
                else
                {
                    Debug.Log("Something went wrong getting all payouts.");
                }
            });
    }

    public void CancelPayout()
    {
        var payoutID = PayoutStatusID.text;
        var accountID = PayoutAccount.text;
        TiliaPaySDK.CancelPayout(accountID, payoutID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Payout canceled.");
                    Debug.Log(JsonConvert.SerializeObject(value, Formatting.Indented));
                }
                else
                {
                    Debug.Log("Something went wrong canceling the payout.");
                }
            });
    }

    public void CreateInvoice()
    {
        var price = Int32.Parse(PurchasePrice.text, CultureInfo.InvariantCulture);
        var product = PurchaseProduct.text;
        var user = PurchaseAccount.text;
        Debug.Log("Attempting purchase of " + product + " by " + user + " for " + price + " cents USD):");
        var newItem = new TiliaLineItem() {
            ProductSKU = product,
            Description = "A cool product you really need.",
            Amount = price,
            Currency = "USD",
            TransactionType = "user_to_integrator",
            ReferenceType = "integrator_product_id",
            ReferenceID = product
        };
        var newPurchase = new TiliaNewInvoice()
        {
            AccountID = user,
            ReferenceType = "product_purchase_id",
            ReferenceID = "invoice_" + product,
            Description = "A great purchase full of cool stuff."
        };
        newPurchase.LineItems.Add(newItem);
        var newPayment = new TiliaPayment()
        {
            Amount = 0,
            ID = PurchasePSP.text
        };
        newPurchase.PaymentMethods.Add(newPayment);
        TiliaPaySDK.CreateInvoice(newPurchase,
            (value) =>
            {
                if (value.Success)
                {
                    PurchaseInvoice.text = value.ID;
                    Debug.Log("Invoice created.");
                }
                else
                {
                    Debug.Log("Something went wrong creating the invoice.");
                }
            });
    }

    public void PayInvoice()
    {
        var invoiceID = PurchaseInvoice.text;
        TiliaPaySDK.PayInvoice(invoiceID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Invoice paid.");
                }
                else
                {
                    Debug.Log("Something went wrong paying the invoice.");
                }
            });
    }

    public void GetInvoice()
    {
        var invoiceID = PurchaseInvoice.text;
        TiliaPaySDK.GetInvoice(invoiceID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Invoice found.");
                    Debug.Log(JsonConvert.SerializeObject(value, Formatting.Indented));
                }
                else
                {
                    Debug.Log("Something went wrong getting the invoice.");
                }
            });
    }

    public void CreateEscrow()
    {
        var price = Int32.Parse(PurchasePrice.text, CultureInfo.InvariantCulture);
        var product = PurchaseProduct.text;
        var user = PurchaseAccount.text;
        Debug.Log("Attempting escrow of " + product + " by " + user + " for " + price + " cents USD):");
        var newItem = new TiliaLineItem()
        {
            ProductSKU = product,
            Description = "A cool product you really need.",
            Amount = price,
            Currency = "USD",
            TransactionType = "user_to_integrator",
            ReferenceType = "integrator_product_id",
            ReferenceID = product
        };
        var newPurchase = new TiliaNewInvoice()
        {
            AccountID = user,
            ReferenceType = "product_purchase_id",
            ReferenceID = "escrow_" + product,
            Description = "A great purchase full of cool stuff."
        };
        newPurchase.LineItems.Add(newItem);
        var newPayment = new TiliaPayment()
        {
            Amount = 0,
            ID = PurchasePSP.text
        };
        newPurchase.PaymentMethods.Add(newPayment);
        TiliaPaySDK.CreateEscrow(newPurchase,
            (value) =>
            {
                if (value.Success)
                {
                    PurchaseInvoice.text = value.ID;
                    Debug.Log("Escrow created.");
                }
                else
                {
                    Debug.Log("Something went wrong creating the escrow.");
                }
            });
    }

    public void PayEscrow()
    {
        var invoiceID = PurchaseInvoice.text;
        TiliaPaySDK.PayEscrow(invoiceID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Escrow paid.");
                }
                else
                {
                    Debug.Log("Something went wrong paying the escrow.");
                }
            });
    }

    public void CommitEscrow()
    {
        var invoiceID = PurchaseInvoice.text;
        TiliaPaySDK.CommitEscrow(invoiceID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Escrow committed.");
                }
                else
                {
                    Debug.Log("Something went wrong comitting the escrow.");
                }
            });
    }

    public void CancelEscrow()
    {
        var invoiceID = PurchaseInvoice.text;
        TiliaPaySDK.CancelEscrow(invoiceID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Escrow canceled.");
                }
                else
                {
                    Debug.Log("Something went wrong canceling the escrow.");
                }
            });
    }

    public void GetEscrow()
    {
        var invoiceID = PurchaseInvoice.text;
        TiliaPaySDK.GetEscrow(invoiceID,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Escrow found.");
                    Debug.Log(JsonConvert.SerializeObject(value, Formatting.Indented));
                }
                else
                {
                    Debug.Log("Something went wrong getting the escrow.");
                }
            });
    }

    public void CopyAccountToPurchase()
    {
        PurchaseAccount.text = AccountInput.text;
    }

    public void AddUserButton()
    {
        var userName = UserNameInput.text;
        var password = PasswordInput.text;
        var email = EmailInput.text;
        Debug.Log("Attempting to register new user " + userName + " (" + email + ") with password: " + password);
        var newUser = new TiliaNewUser()
        {
            UserName = userName,
            Password = password,
            Email = email
        };
        TiliaPaySDK.RegisterUser(newUser,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Completed registration for " + userName + " with account ID " + value.AccountID + "!");
                    AccountInput.text = value.AccountID;
                }
                else
                {
                    Debug.Log("Something went wrong registering user " + userName + ".");
                }
            });
    }

    public void UserInfoButton()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to get user info for " + account);
        TiliaPaySDK.GetUserInfo(account,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Found user info with account ID " + value.ID + "!");
                    AccountInput.text = value.ID;
                }
                else
                {
                    Debug.Log("Something went wrong searching for " + account + ".");
                }
            });
    }

    public void CheckKYCButton()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to check KYC for " + account);
        TiliaPaySDK.CheckKYC(account,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Got user KYC state of " + value.ID + " [" + value.State + "]" + "!");
                }
                else
                {
                    Debug.Log("Something went wrong checking KYC for " + account + ".");
                }
            });
    }

    public void PaymentMethodsButton()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to get payment methods for " + account);
        TiliaPaySDK.GetPaymentMethods(account,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Got payment methods:");
                    Debug.Log(JsonConvert.SerializeObject(value));
                }
                else
                {
                    Debug.Log("Something went wrong getting payment methods for " + account + ".");
                }
            });
    }

    public void UserSearchButton()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to search for user " + account);
        TiliaPaySDK.SearchForUser(account,
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Found user info with account ID " + value.ID + "!");
                    AccountInput.text = value.ID;
                }
                else
                {
                    Debug.Log("Something went wrong searching for " + account + ".");
                }
            });
    }

    public void ClientRedirectButton()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to redirect user " + account);
        TiliaPaySDK.RequestClientRedirectURL(account, new string[] { "user_info" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Client redirect received: " + value.Redirect);
                }
                else
                {
                    Debug.Log("Something went wrong redirecting user " + account + ".");
                }
            });
    }

    public void FlowKYC()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to redirect user " + account);
        TiliaPaySDK.RequestClientRedirectURL(account, new string[] { "user_info", "read_payment_method", "write_payment_method", "read_kyc", "verify_kyc" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Attempting to start KYC widget with redirect: " + value.Redirect);
                    TiliaPaySDK.InitiateKYCWidget(value.Redirect,
                        (widget) =>
                        {
                            if (widget.Completed)
                            {
                                Debug.Log("KYC process completed: " + widget.Result);
                            }
                            else
                            {
                                Debug.Log("KYC process was cancelled.");
                            }
                        });
                }
                else
                {
                    Debug.Log("Something went wrong redirecting user " + account + ".");
                }
            });
    }

    public void FlowTOS()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to redirect user " + account);
        TiliaPaySDK.RequestClientRedirectURL(account, new string[] { "user_info", "read_payment_method", "write_payment_method", "read_kyc", "verify_kyc" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Attempting to start KYC widget with redirect: " + value.Redirect);
                    TiliaPaySDK.InitiateTOSWidget(value.Redirect,
                        (widget) =>
                        {
                            if (widget.Completed)
                            {
                                Debug.Log("TOS process completed.");
                            }
                            else
                            {
                                Debug.Log("TOS process was cancelled.");
                            }
                        });
                }
                else
                {
                    Debug.Log("Something went wrong redirecting user " + account + ".");
                }
            });
    }

    public void FlowPayment()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to redirect user " + account);
        TiliaPaySDK.RequestClientRedirectURL(account, new string[] { "user_info", "read_payment_method", "write_payment_method", "read_kyc", "verify_kyc" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Attempting to start payment widget with redirect: " + value.Redirect);
                    TiliaPaySDK.InitiatePurchaseWidget(value.Redirect,
                        (widget) =>
                        {
                            if (widget.Completed)
                            {
                                PurchasePSP.text = widget.ID;
                                Debug.Log("Purchase process completed: " + widget.ID);
                            }
                            else
                            {
                                Debug.Log("Purchase process was cancelled.");
                            }
                        });
                }
                else
                {
                    Debug.Log("Something went wrong redirecting user " + account + ".");
                }
            });
    }

    public void FlowPayout()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to redirect user " + account);
        TiliaPaySDK.RequestClientRedirectURL(account, new string[] { "user_info", "read_payment_method", "write_payment_method", "read_kyc", "verify_kyc" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Attempting to start KYC widget with redirect: " + value.Redirect);
                    TiliaPaySDK.InitiatePayoutWidget(value.Redirect,
                        (widget) =>
                        {
                            if (widget.Completed)
                            {
                                PurchasePSP.text = widget.ID;
                                Debug.Log("Payout process completed: " + widget.ID);
                            }
                            else
                            {
                                Debug.Log("Payout process was cancelled.");
                            }
                        });
                }
                else
                {
                    Debug.Log("Something went wrong redirecting user " + account + ".");
                }
            });
    }

}
