using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System;
using UnityEngine;
using UnityEngine.UI;
using Tilia.Pay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TiliaTest : MonoBehaviour
{

    public Tilia.Pay.Tilia TiliaPay;

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

    public void CreateInvoice()
    {
        var price = Int32.Parse(PurchasePrice.text, CultureInfo.InvariantCulture);
        var product = PurchaseProduct.text;
        var user = PurchaseAccount.text;
        Debug.Log("Attempting purchase of " + product + " by " + user + " for $" + price + "):");
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
        TiliaPay.CreateInvoice(newPurchase,
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
        TiliaPay.PayInvoice(invoiceID,
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
        TiliaPay.GetInvoice(invoiceID,
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
        TiliaPay.RegisterUser(newUser,
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

    public void JSTest()
    {
        TiliaPay.ExecuteRemoteJS("document.getElementById('mirror').value = 'Testing from Unity'; sendMessage();");
    }

    public void UserInfoButton()
    {
        var account = AccountInput.text;
        Debug.Log("Attempting to get user info for " + account);
        TiliaPay.GetUserInfo(account,
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
        TiliaPay.CheckKYC(account,
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
        TiliaPay.GetPaymentMethods(account,
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
        TiliaPay.SearchForUser(account,
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
        TiliaPay.RequestClientRedirectURL(account, new string[] { "user_info" },
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
        TiliaPay.RequestClientRedirectURL(account, new string[] { "user_info", "read_payment_method", "write_payment_method", "read_kyc", "verify_kyc" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Attempting to start KYC widget with redirect: " + value.Redirect);
                    TiliaPay.InitiateKYCWidget(value.Redirect,
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
        TiliaPay.RequestClientRedirectURL(account, new string[] { "user_info", "read_payment_method", "write_payment_method", "read_kyc", "verify_kyc" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Attempting to start KYC widget with redirect: " + value.Redirect);
                    TiliaPay.InitiateTOSWidget(value.Redirect,
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
        TiliaPay.RequestClientRedirectURL(account, new string[] { "user_info", "read_payment_method", "write_payment_method", "read_kyc", "verify_kyc" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Attempting to start KYC widget with redirect: " + value.Redirect);
                    TiliaPay.InitiatePurchaseWidget(value.Redirect,
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
        TiliaPay.RequestClientRedirectURL(account, new string[] { "user_info", "read_payment_method", "write_payment_method", "read_kyc", "verify_kyc" },
            (value) =>
            {
                if (value.Success)
                {
                    Debug.Log("Attempting to start KYC widget with redirect: " + value.Redirect);
                    TiliaPay.InitiatePayoutWidget(value.Redirect,
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
