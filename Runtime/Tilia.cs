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
        public static readonly string Version = "0.9.1";

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
                    // Testing in editor
                    var IntegratorHTML = Application.dataPath + "/Tilia/TiliaPayIntegrator.html";
                    LogInfo("Defaulting to " + IntegratorHTML);
                    WebBrowser2DComponent.InitialURL = IntegratorHTML;
                    //WebBrowser2DComponent.Navigate(IntegratorHTML);
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

        public void InitiatePurchaseWidget(string redirectURL, Action<TiliaWidgetPurchase> onComplete)
        {
            if (Status.Idle)
            {
                Status.State = TiliaState.PAYMENT_PENDING;
                Status.OnComplete = (value) => { onComplete(new TiliaWidgetPurchase(value)); };
                ExecuteRemoteJS("LoadTiliaWidget('purchase', '" + redirectURL + "');");
            }
        }

        public void InitiatePayoutWidget(string redirectURL, Action<TiliaWidgetPayout> onComplete)
        {
            if (Status.Idle)
            {
                Status.State = TiliaState.PAYOUT_PENDING;
                Status.OnComplete = (value) => { onComplete(new TiliaWidgetPayout(value)); };
                ExecuteRemoteJS("LoadTiliaWidget('payout', '" + redirectURL + "');");
            }
        }

        public void InitiateKYCWidget(string redirectURL, Action<TiliaWidgetKYC> onComplete)
        {
            if (Status.Idle)
            {
                Status.State = TiliaState.KYC_PENDING;
                Status.OnComplete = (value) => { onComplete(new TiliaWidgetKYC(value)); };
                ExecuteRemoteJS("LoadTiliaWidget('kyc', '" + redirectURL + "');");
            }
        }

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

        private void CreateEscrow(TiliaInvoice invoice, Action<TiliaEscrow> onComplete)
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

        public void GetEscrow(string invoiceID, Action<TiliaEscrow> onComplete)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("invoicing", "escrow/" + UnityWebRequest.EscapeURL(invoiceID), "v2"),
                    (value) => { onComplete(new TiliaEscrow(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        private void CreateInvoice(TiliaInvoice invoice, Action<TiliaInvoice> onComplete)
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

        public void RequestClientRedirectURL(string accountID, string[] scopes, Action<TiliaUserAuth> onClientRedirect)
        {
            ValidateAndSend("", () => {
                var jsonData = new JObject(
                    new JProperty("account_id", accountID),
                    new JProperty("scopes", scopes)
                );

                PerformWebPost(
                    MakeRequestURI("auth", "authorize/user"),
                    jsonData.ToString(Newtonsoft.Json.Formatting.None),
                    (value) => { onClientRedirect(new TiliaUserAuth(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        public void RegisterUser(TiliaNewUser user, Action<TiliaRegistration> onRegister)
        {
            ValidateAndSend("", () => {
                PerformWebPost(
                    MakeRequestURI("registration", "register", "v2"),
                    user.Export().ToString(Formatting.None),
                    (value) => {
                        if (StringOrNull(value["status"]) == "Success" && !TokenIsNull(value["payload"]["registration_id"]))
                        {
                            CompleteRegisterUser(value["payload"]["registration_id"].ToString(), onRegister);
                        }
                        else
                        {
                            onRegister(new TiliaRegistration(value));
                        }
                    },
                    AuthTokenForScope("")
               );
            });
        }

        // This should only be used by RegisterUser and should not be publicly accessible.
        // We have converted this two-step process into a one-step process per Tilia's request.
        private void CompleteRegisterUser(string registrationID, Action<TiliaRegistration> onCompleteRegister)
        {
            ValidateAndSend("", () => {
                PerformWebPut(
                    MakeRequestURI("registration", "register/" + UnityWebRequest.EscapeURL(registrationID), "v2"),
                    registrationID,
                    (value) => { onCompleteRegister(new TiliaRegistration(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        public void CheckKYC(string accountID, Action<TiliaKYC> onUserCheck)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("pii", "kyc/" + UnityWebRequest.EscapeURL(accountID), "v1"),
                    (value) => { onUserCheck(new TiliaKYC(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        public void GetPaymentMethods(string accountID, Action<TiliaPaymentMethods> onPaymentMethods)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("payments", UnityWebRequest.EscapeURL(accountID) + "/payment_methods"),
                    (value) => { onPaymentMethods(new TiliaPaymentMethods(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        public void GetUserInfo(string accountID, Action<TiliaUser> onUserInfo)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("accounts", UnityWebRequest.EscapeURL(accountID) + "/user-info", "v1"),
                    (value) => { onUserInfo(new TiliaUser(value)); },
                    AuthTokenForScope("")
               );
            });
        }

        public void SearchForUser(string username, Action<TiliaUser> onUserSearch)
        {
            ValidateAndSend("", () => {
                PerformWebGet(
                    MakeRequestURI("accounts", "user-info/search?username=" + UnityWebRequest.EscapeURL(username), "v1"),
                    (value) => { onUserSearch(new TiliaUser(value)); },
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
            if (!string.IsNullOrEmpty(webRequest.downloadHandler.text))
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
