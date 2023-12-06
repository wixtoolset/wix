// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using SimpleJson;
    using WixToolset.Data.Bind;

    /// <summary>
    /// Object that represents a localization file.
    /// </summary>
    public sealed class Localization
    {
        private readonly Dictionary<string, BindVariable> variables = new Dictionary<string, BindVariable>();
        private readonly Dictionary<string, LocalizedControl> localizedControls = new Dictionary<string, LocalizedControl>();

        /// <summary>
        /// Instantiates a new localization object with default location.
        /// </summary>
        public Localization(int? codepage, int? summaryInformationCodepage, string culture, IDictionary<string, BindVariable> variables, IDictionary<string, LocalizedControl> localizedControls) : 
            this(LocalizationLocation.Source, codepage, summaryInformationCodepage, culture, variables, localizedControls)
        {
        }

        /// <summary>
        /// Instantiates a new localization object.
        /// </summary>
        public Localization(LocalizationLocation location, int? codepage, int? summaryInformationCodepage, string culture, IDictionary<string, BindVariable> variables, IDictionary<string, LocalizedControl> localizedControls)
        {
            this.Location = location;
            this.Codepage = codepage;
            this.SummaryInformationCodepage = summaryInformationCodepage;
            this.Culture = culture?.ToLowerInvariant() ?? String.Empty;
            this.variables = new Dictionary<string, BindVariable>(variables);
            this.localizedControls = new Dictionary<string, LocalizedControl>(localizedControls);
        }

        /// <summary>
        /// Gets the location the localization came from.
        /// </summary>
        public LocalizationLocation Location { get; private set; }

        /// <summary>
        /// Gets the codepage.
        /// </summary>
        /// <value>The codepage.</value>
        public int? Codepage { get; private set; }

        /// <summary>
        /// Gets the summary information codepage.
        /// </summary>
        /// <value>The summary information codepage.</value>
        public int? SummaryInformationCodepage { get; private set; }

        /// <summary>
        /// Gets the culture.
        /// </summary>
        /// <value>The culture.</value>
        public string Culture { get; private set; }

        /// <summary>
        /// Gets the variables.
        /// </summary>
        /// <value>The variables.</value>
        public ICollection<BindVariable> Variables => this.variables.Values;

        /// <summary>
        /// Gets the localized controls.
        /// </summary>
        /// <value>The localized controls.</value>
        public ICollection<KeyValuePair<string, LocalizedControl>> LocalizedControls => this.localizedControls;

        /// <summary>
        /// Updates the location, if the location is a higher state than the current state.
        /// </summary>
        /// <param name="location">Location to update to.</param>
        /// <returns>This localization object.</returns>
        public Localization UpdateLocation(LocalizationLocation location)
        {
            if (this.Location < location)
            {
                this.Location = location;
            }

            return this;
        }

        internal JsonObject Serialize()
        {
            var jsonObject = new JsonObject()
            {
                { "location", this.Location.ToString().ToLowerInvariant() }
            };

            if (this.Codepage.HasValue)
            {
                jsonObject.Add("codepage", this.Codepage.Value);
            }

            if (this.SummaryInformationCodepage.HasValue)
            {
                jsonObject.Add("summaryCodepage", this.SummaryInformationCodepage.Value);
            }

            jsonObject.AddIsNotNullOrEmpty("culture", this.Culture);

            // Serialize bind variables.
            if (this.Variables.Count > 0)
            {
                var variablesJson = new JsonArray(this.Variables.Count);

                foreach (var variable in this.Variables)
                {
                    var variableJson = variable.Serialize();

                    variablesJson.Add(variableJson);
                }

                jsonObject.Add("variables", variablesJson);
            }

            // Serialize localized control.
            if (this.LocalizedControls.Count > 0)
            {
                var controlsJson = new JsonObject();

                foreach (var controlWithKey in this.LocalizedControls)
                {
                    var controlJson = controlWithKey.Value.Serialize();

                    controlsJson.Add(controlWithKey.Key, controlJson);
                }

                jsonObject.Add("controls", controlsJson);
            }

            return jsonObject;
        }

        internal static Localization Deserialize(JsonObject jsonObject)
        {
            var location = jsonObject.GetEnumOrDefault("location", LocalizationLocation.Source);
            var codepage = jsonObject.GetValueOrDefault("codepage", null);
            var summaryCodepage = jsonObject.GetValueOrDefault("summaryCodepage", null);
            var culture = jsonObject.GetValueOrDefault<string>("culture");

            var variables = new Dictionary<string, BindVariable>();
            var variablesJson = jsonObject.GetValueOrDefault("variables", new JsonArray());
            foreach (JsonObject variableJson in variablesJson)
            {
                var bindPath = BindVariable.Deserialize(variableJson);
                variables.Add(bindPath.Id, bindPath);
            }

            var controls = new Dictionary<string, LocalizedControl>();
            var controlsJson = jsonObject.GetValueOrDefault<JsonObject>("controls");
            if (controlsJson != null)
            {
                foreach (var controlJsonWithKey in controlsJson)
                {
                    var control = LocalizedControl.Deserialize((JsonObject)controlJsonWithKey.Value);
                    controls.Add(controlJsonWithKey.Key, control);
                }
            }

            return new Localization(location, codepage, summaryCodepage, culture, variables, controls);
        }
    }
}
