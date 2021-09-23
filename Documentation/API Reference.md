# TiliaPay Unity SDK API Reference

## General API Functions

### public void CreatePayout(string accountID, TiliaNewPayout payout, Action<TiliaPayout> onComplete)
* accountID: User account ID that the payout is being requested for.
* payout: Full payout details defined by a TiliaNewPayout object class.
* onComplete: Action callback event. Callback happens on both success and failure.

Create a new payout request.

### public void GetPayout(string accountID, string payoutID, Action<TiliaPayout> onComplete)
* accountID: User account ID that the payout was previously requested for.
* payoutID: The ID of payout request that you want details about.
* onComplete: Action callback event. Callback happens on both success and failure.

Retrieve details about an existing payout request.

### public void GetPayouts(string accountID, Action<TiliaPayouts> onComplete)
* accountID: User account ID that you want payout information from.
* onComplete: Action callback event. Callback happens on both success and failure.

Get all payout requests associated with a specific user account.

### public void CancelPayout(string accountID, string payoutID, Action<TiliaPayout> onComplete)
* accountID: User account ID that the payout request is associated with.
* payoutID: The ID of payout request that you want to cancel.
* onComplete: Action callback event. Callback happens on both success and failure.

Cancel a previously created payout request.

### public void CreateEscrow(TiliaNewInvoice invoice, Action<TiliaEscrow> onComplete)
* invoice: All the necessary details for a new Escrow passed as a TiliaNewInvoice object.
* onComplete: Action callback event. Callback happens on both success and failure.

Create a new escrow invoice. Invoice is not paid or committed automatically, this just creates it as an open invoice.

### public void PayEscrow(string escrowID, Action<TiliaEscrow> onComplete)
* escrowID: The ID of a previously created escrow to pay.
* onComplete: Action callback event. Callback happens on both success and failure.

Pay a previously created escrow invoice.

### public void CommitEscrow(string escrowID, Action<TiliaEscrow> onComplete)
* escrowID: The ID of a previously created escrow to commit.
* onComplete: Action callback event. Callback happens on both success and failure.

Commit a previously created escrow invoice.

### public void CancelEscrow(string escrowID, Action<TiliaEscrow> onComplete)
* escrowID: The ID of a previously created escrow to cancel.
* onComplete: Action callback event. Callback happens on both success and failure.

Cancels a previously created escrow invoice.

### public void GetEscrow(string escrowID, Action<TiliaEscrow> onComplete)
* escrowID: The ID of a previously created escrow to retrieve.
* onComplete: Action callback event. Callback happens on both success and failure.

Retrieve details about a previously created escrow invoice.

### public void CreateInvoice(TiliaNewInvoice invoice, Action<TiliaInvoice> onComplete)
* invoice: All the necessary details for a new invoice passed as a TiliaNewInvoice object.
* onComplete: Action callback event. Callback happens on both success and failure.

Create a new standard purchase invoice. Invoice is not paid automatically, this just creates it as an open invoice.

### public void PayInvoice(string invoiceID, Action<TiliaInvoice> onComplete)
* invoiceID: The ID of the previously created invoice you want to pay.
* onComplete: Action callback event. Callback happens on both success and failure.

Pay a previously created purchase invoice.

### public void GetInvoice(string invoiceID, Action<TiliaInvoice> onComplete)
* invoiceID: The ID of the previously created invoice you want to retrieve.
* onComplete: Action callback event. Callback happens on both success and failure.

Retrieve details about a previously created purchase invoice.

### public void RequestClientRedirectURL(string accountID, string[] scopes, Action<TiliaUserAuth> onComplete)
* accountID: The ID of the user account which will be accessing the widget flow.
* scopes: An array of scope permissions that need to be granted for the desired widget flow.
* onComplete: Action callback event. Callback happens on both success and failure.

Request a client redirect URL for use in the widget flow process.

### public void RegisterUser(TiliaNewUser user, Action<TiliaRegistration> onComplete)
* user: All details necessary to create a new account passed in as a TiliaNewUser object.
* onComplete: Action callback event. Callback happens on both success and failure.

Create a new user account.

### public void CheckKYC(string accountID, Action<TiliaKYC> onComplete)
* accountID: The account ID of the user you want to check.
* onComplete: Action callback event. Callback happens on both success and failure.

Check the KYC (Know Your Customer) status of a user account. In other words, have they filled in their contact information yet.

### public void GetPaymentMethods(string accountID, Action<TiliaPaymentMethods> onComplete)
* accountID: The account ID of the user you want to retrieve payment methods for.
* onComplete: Action callback event. Callback happens on both success and failure.

Retrieve a list of all known payment methods associated with a user account, including Tilia wallet balance.

### public void GetUserInfo(string accountID, Action<TiliaUser> onComplete)
* accountID: The account ID of the user you want to retrieve.
* onComplete: Action callback event. Callback happens on both success and failure.

Retrieve full user profile for a given account ID.

### public void SearchForUser(string username, Action<TiliaUser> onComplete)
* username: The username of the user you want to retrieve.
* onComplete: Action callback event. Callback happens on both success and failure.

Retrieve full user profile for a given username.

## Tilia Browser Widget Functions

### public void InitiatePurchaseWidget(string redirectURL, Action<TiliaWidgetPurchase> onComplete)
* redirectURL: The URL returned by the RequestClientRedirectURL function.
* onComplete: Action callback event. Callback happens on both success and cancellation by user.

Inititialize the web browser Tilia widget for the purchase flow.

### public void InitiatePayoutWidget(string redirectURL, Action<TiliaWidgetPayout> onComplete)
* redirectURL: The URL returned by the RequestClientRedirectURL function.
* onComplete: Action callback event. Callback happens on both success and cancellation by user.

Inititialize the web browser Tilia widget for the payout flow.

### public void InitiateKYCWidget(string redirectURL, Action<TiliaWidgetKYC> onComplete)
* redirectURL: The URL returned by the RequestClientRedirectURL function.
* onComplete: Action callback event. Callback happens on both success and cancellation by user.

Inititialize the web browser Tilia widget for the KYC flow.

### public void InitiateTOSWidget(string redirectURL, Action<TiliaWidgetTOS> onComplete)
* redirectURL: The URL returned by the RequestClientRedirectURL function.
* onComplete: Action callback event. Callback happens on both success and cancellation by user.

Inititialize the web browser Tilia widget for the TOS flow.
