using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleWebBrowser;

namespace Tilia.Pay
{
    public class Tilia : MonoBehaviour
    {
        public static readonly string Version = "0.9.3";

        public bool StagingEnvironment = true;

        public string ClientID;
        public string ClientSecret;

        public string StagingURI = "staging.tilia-inc.com";
        public string ProductionURI = "tilia-inc.com";

        public GameObject WebBrowser;

        #region Private Variables
        private WebBrowser2D WebBrowser2DComponent;

        private TiliaToken AuthToken = new TiliaToken();

        private TiliaStatus Status = new TiliaStatus();

        private enum TiliaState
        {
            IDLE,
            PAYMENT_PENDING,
            PAYOUT_PENDING,
            KYC_PENDING,
            TOS_PENDING
        }

        [Serializable]
        private class TiliaStatus
        {
            public TiliaState State = TiliaState.IDLE;
            public Action<JToken> OnComplete;

            public void Clear()
            {
                State = TiliaState.IDLE;
                OnComplete = null;
            }

            public bool Idle
            {
                get { return State == TiliaState.IDLE; }
            }
        }

        #endregion

        #region URI
        internal string BaseURI
        {
            get
            {
                if (StagingEnvironment)
                {
                    return StagingURI;
                }
                else
                {
                    return ProductionURI;
                }
            }
        }

        internal string MakeRequestURI(string service, string resource, string version = null)
        {
            if (String.IsNullOrEmpty(version))
            {
                return "https://" + service + "." + BaseURI + "/" + resource;
            }
            else
            {
                return "https://" + service + "." + BaseURI + "/" + version + "/" + resource;
            }
        }
        #endregion

        #region Unity Functions
        // Start is called before the first frame update
        void Awake()
        {
            if (WebBrowser != null)
            {
                WebBrowser2DComponent = WebBrowser.GetComponentInChildren<SimpleWebBrowser.WebBrowser2D>();
                if (WebBrowser2DComponent == null)
                {
                    LogError("Failed to find WebBrowser2D component.");
                }
                else
                {
                    WebBrowser2DComponent.OnJSQuery += OnJSQuery;
                    WebBrowser2DComponent.OnPageLoaded += OnPageLoaded;
#if UNITY_EDITOR
                    // Testing in editor
                    string GUIDPath = UnityEditor.AssetDatabase.GUIDToAssetPath("f1e36d4d93e14744686f94299e2cc6aa").Substring(7);
                    var IntegratorHTML = System.IO.Path.Combine(Application.dataPath, GUIDPath);
                    LogInfo("Defaulting to " + IntegratorHTML);
                    WebBrowser2DComponent.InitialURL = IntegratorHTML;
#endif
                }

                // Start web browser off in inactive state until we need it.
                //WebBrowser.SetActive(false);
            }
            else
            {
                LogError("No browser UI game object specified.");
            }
        }
        #endregion

        #region Browser Functions

        private void OnPageLoaded(string url)
        {
            LogInfo("Browser is at new URL: " + url);
            var index = url.IndexOf('#');
            if (index != -1)
            {
                var payload = url.Substring(index + 1);
                LogInfo("Found payload: " + payload);
                try
                {
                    var json = JObject.Parse(payload);
                    LogInfo("Payload returned for: " + json["result"].ToString());
                    HandleWebPayload(json);
                }
                catch (JsonReaderException ex)
                {
                    LogError("Payload not in JSON format: " + ex.Message);
                }
            }
        }

        private void OnJSQuery(string query)
        {
            LogInfo("Javascript query: " + query);
            WebBrowser2DComponent.RespondToJSQuery("My response: OK");
        }

        public void ShowBrowser(string url)
        {
            if (WebBrowser == null || WebBrowser2DComponent == null)
                return;

            WebBrowser.SetActive(true);
            if (WebBrowser2DComponent != null)
            {
                WebBrowser2DComponent.Navigate(url);
            }
        }

        public void HideBrowser()
        {
            if (WebBrowser == null || WebBrowser2DComponent == null)
                return;

            WebBrowser.SetActive(false);
        }

        public void ExecuteRemoteJS(string js)
        {
            if (WebBrowser == null || WebBrowser2DComponent == null)
                return;

            LogInfo("Executing remote JS: " + js);
            WebBrowser2DComponent.RunJavaScript(js);
        }

        #endregion

        #region Web Widget Functions

        internal void HandleWebPayload(JObject json)
        {
            var flow = json["result"].ToString();
            var payload = json["payload"];
            LogInfo("Attempting to handle web payload [" + flow + "]: " + payload.ToString(Formatting.Indented));
            if (!Status.Idle)
            {
                // Getting a return about payments.
                // Payment flow continuing.
                Status.OnComplete(payload);
                Status.Clear();
            }
        }

        /// <summary>
        /// Inititialize the web browser Tilia widget for the purchase flow.
        /// </summary>
        /// <param name="redirectURL">The URL returned by the RequestClientRedirectURL function.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and cancellation by user.</param>
        public void InitiatePurchaseWidget(string redirectURL, Action<TiliaWidgetPurchase> onComplete)
        {
            if (Status.Idle)
            {
                Status.State = TiliaState.PAYMENT_PENDING;
                Status.OnComplete = (value) => { onComplete(new TiliaWidgetPurchase(value)); };
                ExecuteRemoteJS("LoadTiliaWidget('purchase', '" + redirectURL + "');");
            }
        }

        /// <summary>
        /// Inititialize the web browser Tilia widget for the payout flow.
        /// </summary>
        /// <param name="redirectURL">The URL returned by the RequestClientRedirectURL function.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and cancellation by user.</param>
        public void InitiatePayoutWidget(string redirectURL, Action<TiliaWidgetPayout> onComplete)
        {
            if (Status.Idle)
            {
                Status.State = TiliaState.PAYOUT_PENDING;
                Status.OnComplete = (value) => { onComplete(new TiliaWidgetPayout(value)); };
                ExecuteRemoteJS("LoadTiliaWidget('payout', '" + redirectURL + "');");
            }
        }

        /// <summary>
        /// Inititialize the web browser Tilia widget for the KYC flow.
        /// </summary>
        /// <param name="redirectURL">The URL returned by the RequestClientRedirectURL function.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and cancellation by user.</param>
        public void InitiateKYCWidget(string redirectURL, Action<TiliaWidgetKYC> onComplete)
        {
            if (Status.Idle)
            {
                Status.State = TiliaState.KYC_PENDING;
                Status.OnComplete = (value) => { onComplete(new TiliaWidgetKYC(value)); };
                ExecuteRemoteJS("LoadTiliaWidget('kyc', '" + redirectURL + "');");
            }
        }

        /// <summary>
        /// Inititialize the web browser Tilia widget for the TOS flow.
        /// </summary>
        /// <param name="redirectURL">The URL returned by the RequestClientRedirectURL function.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and cancellation by user.</param>
        public void InitiateTOSWidget(string redirectURL, Action<TiliaWidgetTOS> onComplete)
        {
            if (Status.Idle)
            {
                Status.State = TiliaState.TOS_PENDING;
                Status.OnComplete = (value) => { onComplete(new TiliaWidgetTOS(value)); };
                ExecuteRemoteJS("LoadTiliaWidget('tos', '" + redirectURL + "');");
            }
        }

        #endregion

        #region API Functions

        /// <summary>
        /// Create a new payout request.
        /// </summary>
        /// <param name="accountID">User account ID that the payout is being requested for.</param>
        /// <param name="payout">Full payout details defined by a TiliaNewPayout object class.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        private void CreatePayout(string accountID, TiliaNewPayout payout, Action<TiliaPayout> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("invoicing", UnityWebRequest.EscapeURL(accountID) + "/payout", "v2"),
                    payout.Export().ToString(Formatting.None),
                    (value) => { onComplete(new TiliaPayout(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Retrieve details about an existing payout request.
        /// </summary>
        /// <param name="accountID">User account ID that the payout was previously requested for.</param>
        /// <param name="payoutID">The ID of payout request that you want details about.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void GetPayout(string accountID, string payoutID, Action<TiliaPayout> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("invoicing", UnityWebRequest.EscapeURL(accountID) + "/payout/" + UnityWebRequest.EscapeURL(payoutID), "v2"),
                    (value) => { onComplete(new TiliaPayout(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Get all payout requests associated with a specific user account.
        /// </summary>
        /// <param name="accountID">User account ID that you want payout information from.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void GetPayouts(string accountID, Action<TiliaPayouts> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("invoicing", UnityWebRequest.EscapeURL(accountID) + "/payouts", "v2"),
                    (value) => { onComplete(new TiliaPayouts(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Cancel a previously created payout request.
        /// </summary>
        /// <param name="accountID">User account ID that the payout request is associated with.</param>
        /// <param name="payoutID">The ID of payout request that you want to cancel.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void CancelPayout(string accountID, string payoutID, Action<TiliaPayout> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebDelete(
                    MakeRequestURI("invoicing", UnityWebRequest.EscapeURL(accountID) + "/payout/" + UnityWebRequest.EscapeURL(payoutID), "v2"),
                    (value) => { onComplete(new TiliaPayout(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Create a new escrow invoice. Invoice is not paid or committed automatically, this just creates it as an open invoice.
        /// </summary>
        /// <param name="invoice">All the necessary details for a new Escrow passed as a TiliaNewInvoice object.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void CreateEscrow(TiliaNewInvoice invoice, Action<TiliaEscrow> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("invoicing", "escrow", "v2"),
                    invoice.Export().ToString(Formatting.None),
                    (value) => { onComplete(new TiliaEscrow(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Pay a previously created escrow invoice.
        /// </summary>
        /// <param name="escrowID">The ID of a previously created escrow to pay.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void PayEscrow(string escrowID, Action<TiliaEscrow> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("invoicing", "escrow/" + UnityWebRequest.EscapeURL(escrowID) + "/pay", "v2"),
                    "",
                    (value) => { onComplete(new TiliaEscrow(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Commit a previously created escrow invoice.
        /// </summary>
        /// <param name="escrowID">The ID of a previously created escrow to commit.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void CommitEscrow(string escrowID, Action<TiliaEscrow> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("invoicing", "escrow/" + UnityWebRequest.EscapeURL(escrowID) + "/commit", "v2"),
                    "",
                    (value) => { onComplete(new TiliaEscrow(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Cancels a previously created escrow invoice.
        /// </summary>
        /// <param name="escrowID">The ID of a previously created escrow to cancel.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void CancelEscrow(string escrowID, Action<TiliaEscrow> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("invoicing", "escrow/" + UnityWebRequest.EscapeURL(escrowID) + "/cancel", "v2"),
                    "",
                    (value) => { onComplete(new TiliaEscrow(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Retrieve details about a previously created escrow invoice.
        /// </summary>
        /// <param name="escrowID">The ID of a previously created escrow to retrieve.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void GetEscrow(string escrowID, Action<TiliaEscrow> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("invoicing", "escrow/" + UnityWebRequest.EscapeURL(escrowID), "v2"),
                    (value) => { onComplete(new TiliaEscrow(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Create a new standard purchase invoice. Invoice is not paid automatically, this just creates it as an open invoice.
        /// </summary>
        /// <param name="invoice">All the necessary details for a new invoice passed as a TiliaNewInvoice object.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void CreateInvoice(TiliaNewInvoice invoice, Action<TiliaInvoice> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("invoicing", "invoice", "v2"),
                    invoice.Export().ToString(Formatting.None),
                    (value) => { onComplete(new TiliaInvoice(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Pay a previously created purchase invoice.
        /// </summary>
        /// <param name="invoiceID">The ID of the previously created invoice you want to pay.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void PayInvoice(string invoiceID, Action<TiliaInvoice> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("invoicing", "invoice/" + UnityWebRequest.EscapeURL(invoiceID) + "/pay", "v2"),
                    "",
                    (value) => { onComplete(new TiliaInvoice(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Retrieve details about a previously created purchase invoice.
        /// </summary>
        /// <param name="invoiceID">The ID of the previously created invoice you want to retrieve.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void GetInvoice(string invoiceID, Action<TiliaInvoice> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("invoicing", "invoice/" + UnityWebRequest.EscapeURL(invoiceID), "v2"),
                    (value) => { onComplete(new TiliaInvoice(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Request a client redirect URL for use in the widget flow process.
        /// </summary>
        /// <param name="accountID">The ID of the user account which will be accessing the widget flow.</param>
        /// <param name="scopes">An array of scope permissions that need to be granted for the desired widget flow.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void RequestClientRedirectURL(string accountID, string[] scopes, Action<TiliaUserAuth> onComplete)
        {
            ValidateAndSend("", () => {
                var jsonData = new JObject(
                    new JProperty("account_id", accountID),
                    new JProperty("scopes", scopes)
                );

                PerformWebPost(
                    MakeRequestURI("auth", "authorize/user"),
                    jsonData.ToString(Newtonsoft.Json.Formatting.None),
                    (value) => { onComplete(new TiliaUserAuth(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Create a new user account.
        /// </summary>
        /// <param name="user">All details necessary to create a new account passed in as a TiliaNewUser object.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void RegisterUser(TiliaNewUser user, Action<TiliaRegistration> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("registration", "register", "v2"),
                    user.Export().ToString(Formatting.None),
                    (value) => {
                        if (StringOrNull(value["status"]) == "Success" && !TokenIsNull(value["payload"]["registration_id"]))
                        {
                            CompleteRegisterUser(value["payload"]["registration_id"].ToString(), onComplete);
                        }
                        else
                        {
                            onComplete(new TiliaRegistration(value));
                        }
                    },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// This should only be used by RegisterUser and should not be publicly accessible.
        /// We have converted this two-step process into a one-step process per Tilia's request.
        /// </summary>
        /// <param name="registrationID">The registration ID returned by RegisterUser to complete a registration.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        private void CompleteRegisterUser(string registrationID, Action<TiliaRegistration> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebPut(
                    MakeRequestURI("registration", "register/" + UnityWebRequest.EscapeURL(registrationID), "v2"),
                    registrationID,
                    (value) => { onComplete(new TiliaRegistration(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Create the KYC (Know Your Customer) status of a user account. In other words, have they filled in their contact information yet.
        /// </summary>
        /// <param name="accountID">The account ID of the user you want to check.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void CheckKYC(string accountID, Action<TiliaKYC> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("pii", "kyc/" + UnityWebRequest.EscapeURL(accountID), "v1"),
                    (value) => { onComplete(new TiliaKYC(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Retrieve a list of all known payment methods associated with a user account, including Tilia wallet balance.
        /// </summary>
        /// <param name="accountID">The account ID of the user you want to retrieve payment methods for.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void GetPaymentMethods(string accountID, Action<TiliaPaymentMethods> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("payments", UnityWebRequest.EscapeURL(accountID) + "/payment_methods"),
                    (value) => { onComplete(new TiliaPaymentMethods(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Retrieve full user profile for a given account ID.
        /// </summary>
        /// <param name="accountID">The account ID of the user you want to retrieve.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void GetUserInfo(string accountID, Action<TiliaUser> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("accounts", UnityWebRequest.EscapeURL(accountID) + "/user-info", "v1"),
                    (value) => { onComplete(new TiliaUser(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        /// <summary>
        /// Retrieve full user profile for a given username.
        /// </summary>
        /// <param name="username">The username of the user you want to retrieve.</param>
        /// <param name="onComplete">Action callback event. Callback happens on both success and failure.</param>
        public void SearchForUser(string username, Action<TiliaUser> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("accounts", "user-info/search?username=" + UnityWebRequest.EscapeURL(username), "v1"),
                    (value) => { onComplete(new TiliaUser(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        #endregion

        #region Static Helper Functions
        public static bool TokenIsNull(JToken token)
        {
            return token == null || token.Type == JTokenType.Null;
        }

        public static string StringOrNull(JToken value)
        {
            return !TokenIsNull(value) ? value.ToString() : null;
        }

        internal static void LogError(string error)
        {
            Debug.LogError("Tilia ERROR: " + error);
        }

        internal static void LogInfo(string info)
        {
            Debug.LogWarning("Tilia INFO: " + info);
        }
        #endregion

        #region Auth Tokens
        internal void ValidateAndSend(string scope, Action onValidToken)
        {
            if (String.IsNullOrEmpty(scope))
            {
                // Universal privs, for testing
                scope = "change_accounts,search_accounts,read_payment_methods,write_registrations,write_user_tokens,user_info,block_accounts,read_invoices,write_invoices,read_process_credits,write_process_credits,read_kycs";
            }
            var token = AuthTokenForScope(scope);
            if (token != null && token.IsValid())
            {
                onValidToken();
            }
            else
            {
                GetAuthToken(scope, onValidToken);
            }
        }

        private TiliaToken AuthTokenForScope(string scope)
        {
            return AuthToken;
        }

        private void GetAuthToken(string scope, Action onValidToken = null)
        {
            PerformWebPost(MakeRequestURI("auth", "token") + "?client_id=" + UnityWebRequest.EscapeURL(ClientID) + "&client_secret=" + UnityWebRequest.EscapeURL(ClientSecret) + "&grant_type=client_credentials&scope=" + UnityWebRequest.EscapeURL(scope), "",
                (value) =>
                {
                    HandleNewAuthToken(new TiliaToken(value));
                    onValidToken();
                },
                contentType: "application/x-www-form-urlencoded");
        }

        private void HandleNewAuthToken(TiliaToken newToken)
        {
            AuthToken = newToken;
            LogInfo("New auth token stored: " + JsonConvert.SerializeObject(AuthToken));
            if (AuthToken.IsValid())
            {
                LogInfo("New auth token is valid.");
            }
        }
        #endregion

        #region Web Requests
        internal void PerformWebPost(string requestURI, string postData, Action<JObject> onComplete, TiliaToken authToken = null, string contentType = "application/json")
        {
            StartCoroutine(DoWebRoutine(UnityWebRequest.kHttpVerbPOST, requestURI, contentType, postData, onComplete, authToken));
        }

        internal void PerformWebPut(string requestURI, string putData, Action<JObject> onComplete, TiliaToken authToken = null, string contentType = "application/json")
        {
            StartCoroutine(DoWebRoutine(UnityWebRequest.kHttpVerbPUT, requestURI, contentType, putData, onComplete, authToken));
        }

        internal void PerformWebGet(string requestURI, Action<JObject> onComplete, TiliaToken authToken = null)
        {
            StartCoroutine(DoWebRoutine(UnityWebRequest.kHttpVerbGET, requestURI, "", "", onComplete, authToken));
        }

        internal void PerformWebDelete(string requestURI, Action<JObject> onComplete, TiliaToken authToken = null)
        {
            StartCoroutine(DoWebRoutine(UnityWebRequest.kHttpVerbDELETE, requestURI, "", "", onComplete, authToken));
        }

        private IEnumerator DoWebRoutine(string method, string requestURI, string contentType, string postData, Action<JObject> onComplete, TiliaToken authToken = null)
        {
            LogInfo("Attempting to " + method + " " + requestURI + " (" + contentType + "): " + postData);
            UnityWebRequest webRequest;
            if (String.IsNullOrEmpty(postData))
            {
                if (method == UnityWebRequest.kHttpVerbPOST)
                {
                    webRequest = UnityWebRequest.Post(requestURI, postData);
                }
                else if (method == UnityWebRequest.kHttpVerbDELETE)
                {
                    webRequest = UnityWebRequest.Delete(requestURI);
                }
                else if (method == UnityWebRequest.kHttpVerbPUT)
                {
                    webRequest = UnityWebRequest.Put(requestURI, postData);
                }
                else
                {
                    webRequest = UnityWebRequest.Get(requestURI);
                }
            }
            else
            {
                webRequest = UnityWebRequest.Put(requestURI, postData);
                webRequest.method = method;
            }
            if (!String.IsNullOrEmpty(contentType))
            {
                webRequest.SetRequestHeader("Content-Type", contentType);
            }
            webRequest.SetRequestHeader("Accept", "application/json");
            if (authToken != null && authToken.IsValid())
            {
                //LogInfo("Using authtoken: " + authToken.type + " " + authToken.token);
                webRequest.SetRequestHeader("Authorization", authToken.TokenType + " " + authToken.AccessToken);
            }

            // Custom Tilia headers
            webRequest.SetRequestHeader("X-Tilia-Unity-SDK-Version", Version);
            webRequest.SetRequestHeader("X-Tilia-Unity-Version", Application.unityVersion);
            webRequest.SetRequestHeader("X-Tilia-Unity-Platform", Application.platform.ToString());
            webRequest.SetRequestHeader("X-Tilia-Unity-Product-Name", Application.productName);
            webRequest.SetRequestHeader("X-Tilia-Unity-Product-Version", Application.version);

            yield return webRequest.SendWebRequest();

            if (!string.IsNullOrEmpty(webRequest.error))
            {
                LogError("Web " + method + " (" + webRequest.responseCode + ") error: " + webRequest.error);
            }
            if (webRequest.responseCode == 500)
            {
                // Special handling for 500 server errors, since we won't get a payload back from these but
                // we don't want to leave the client hanging with no response at all.
                var json = new JObject(
                    new JProperty("status", "Failed"),
                    new JProperty("web_response_code", webRequest.responseCode)
                );
                onComplete(json);
            }
            else if (!string.IsNullOrEmpty(webRequest.downloadHandler.text))
            {
                LogInfo("Web " + method + " (" + webRequest.responseCode + ") data: " + webRequest.downloadHandler.text);
                try
                {
                    // Convert into JSON class object.
                    var json = JObject.Parse(webRequest.downloadHandler.text);

                    // Capture the response code we got from the server.
                    json.Add(new JProperty("web_response_code", webRequest.responseCode));

                    // Pass to onComplete function.
                    onComplete(json);
                }
                catch (JsonReaderException ex)
                {
                    LogError("JSON parse error: " + ex.Message);
                }
            }
        }
        #endregion
    }
}
