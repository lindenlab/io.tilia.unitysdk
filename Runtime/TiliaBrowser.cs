using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tilia
{
    public abstract class TiliaBrowser : MonoBehaviour
    {
        [Header("SDK Reference (Required)")]
        [Tooltip("If not specified, we will try to find it ourselves, but please don't make us do that.")]
        public TiliaPay TiliaPaySDK;

        public void Awake()
        {
            if (TiliaPaySDK == null)
            {
                // If we don't have one specified, try to find one.
                TiliaPaySDK = FindObjectOfType<TiliaPay>();
                if (TiliaPaySDK == null)
                {
                    LogInfo("Unable to find TiliaPay component in scene.");
                }
            }

            // Let derived classes do their initializtion routine for specific browser.
            Initialize();

            if (TiliaPaySDK != null)
            {
                LogInfo("Using widget " + TiliaPaySDK.WidgetURL);
                SetURL(TiliaPaySDK.WidgetURL);
            }
        }

        public void BeginFlow(string flow, string redirectURL)
        {
            ExecuteRemoteJS("LoadTiliaWidget('" + flow + "', '" + redirectURL + "');");
        }

        #region Abstract Functions
        public abstract void Initialize();

        public abstract void SetURL(string url);

        public abstract void ExecuteRemoteJS(string js);
        #endregion

        #region Helper Functions
        public static bool TokenIsNull(JToken token)
        {
            return token == null || token.Type == JTokenType.Null;
        }

        public static string StringOrNull(JToken value)
        {
            return !TokenIsNull(value) ? value.ToString() : null;
        }

        internal void LogError(string error)
        {
            Debug.LogError("Tilia Browser ERROR: " + error);
        }

        internal void LogInfo(string info)
        {
            if (TiliaPaySDK == null || TiliaPaySDK.LoggingEnabled)
            {
                Debug.LogError("Tilia Browser INFO: " + info);
            }
        }
        #endregion

        #region Payload Handlers
        public void HandleWebPayload(JObject json)
        {
            if (TiliaPaySDK != null)
            {
                TiliaPaySDK.HandleWebPayload(json);
            }
        }

        public void OnPageLoaded(string url)
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
                    if (!TokenIsNull(json) && !TokenIsNull(json["result"]))
                    {
                        LogInfo("Payload returned for: " + json["result"].ToString());
                        HandleWebPayload(json);
                    }
                }
                catch (JsonReaderException ex)
                {
                    LogError("Payload not in JSON format: " + ex.Message);
                }
            }
        }

        public void OnJSQuery(string query)
        {
            LogInfo("Javascript query: " + query);
            try
            {
                var json = JObject.Parse(query);
                if (!TokenIsNull(json) && !TokenIsNull(json["result"]))
                {
                    LogInfo("Javascript payload returned for: " + json["result"].ToString());
                    HandleWebPayload(json);
                }
            }
            catch (JsonReaderException ex)
            {
                LogError("Javascript payload not in JSON format: " + ex.Message);
            }
        }
        #endregion
    }
}