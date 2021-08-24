using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tilia.Pay
{
    [Serializable]
    public class TiliaWidgetResponse
    {
        public string State;
        public string Source;
        public string Event;

        // Captured for debug purposes.
        //public JToken raw_json;

        public bool Canceled
        {
            get { return State == "cancel"; }
        }

        public bool Completed
        {
            get { return State == "complete"; }
        }

        public TiliaWidgetResponse(JToken import)
        {
            // Allow to be created with a direct payload injection.
            Import(import);
        }

        // Will be overridden by children classes to add unique payload processing,
        // but all returns share these bits in common. Always remember to Base.Import()
        // in overrides.
        public virtual void Import(JToken json)
        {
            // Captured for debug purposes.
            //raw_json = json;

            State = Tilia.StringOrNull(json["state"]);
            Source = Tilia.StringOrNull(json["source"]);
            Event = Tilia.StringOrNull(json["event"]);
        }
    }

    [Serializable]
    public class TiliaWidgetPurchase : TiliaWidgetResponse
    {
        public string ID;
        public string PSPReference;
        public string PMState;

        public TiliaWidgetPurchase(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            ID = Tilia.StringOrNull(json["id"]);
            PSPReference = Tilia.StringOrNull(json["psp_reference"]);
            PMState = Tilia.StringOrNull(json["pm_state"]);
        }
    }

    [Serializable]
    public class TiliaWidgetPayout : TiliaWidgetResponse
    {
        public string ID;
        public string PSPReference;
        public string PMState;

        public TiliaWidgetPayout(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            ID = Tilia.StringOrNull(json["id"]);
            PSPReference = Tilia.StringOrNull(json["psp_reference"]);
            PMState = Tilia.StringOrNull(json["pm_state"]);
        }
    }

    [Serializable]
    public class TiliaWidgetKYC : TiliaWidgetResponse
    {
        public string Result;

        public TiliaWidgetKYC(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);

            Result = Tilia.StringOrNull(json["result"]);
        }
    }

    [Serializable]
    public class TiliaWidgetTOS : TiliaWidgetResponse
    {
        // TOS returns no special values currently, according to API reference.

        public TiliaWidgetTOS(JToken import) : base(import)
        {
            // Nothing special here. Just has to be defined.
        }

        // Override base importer for unique payload elements.
        public override void Import(JToken json)
        {
            // Let it do the common elements all returns have.
            base.Import(json);
        }
    }
}