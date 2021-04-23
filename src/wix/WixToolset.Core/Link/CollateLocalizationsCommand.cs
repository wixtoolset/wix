// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;

    internal class CollateLocalizationsCommand
    {
        public CollateLocalizationsCommand(IMessaging messaging, IEnumerable<Localization> localizations)
        {
            this.Messaging = messaging;
            this.Localizations = localizations;
        }

        private IMessaging Messaging { get; }

        private IEnumerable<Localization> Localizations { get; }

        public Dictionary<string, Localization> Execute()
        {
            var localizationsByCulture = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);

            foreach (var localization in this.Localizations)
            {
                if (localizationsByCulture.TryGetValue(localization.Culture, out var existingCulture))
                {
                    var merged = this.Merge(existingCulture, localization);
                    localizationsByCulture[localization.Culture] = merged;
                }
                else
                {
                    localizationsByCulture.Add(localization.Culture, localization);
                }
            }

            return localizationsByCulture;
        }

        private Localization Merge(Localization existingLocalization, Localization localization)
        {
            var variables = existingLocalization.Variables.ToDictionary(v => v.Id);
            var controls = existingLocalization.LocalizedControls.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach (var newVariable in localization.Variables)
            {
                if (!variables.TryGetValue(newVariable.Id, out var existingVariable) || (existingVariable.Overridable && !newVariable.Overridable))
                {
                    variables[newVariable.Id] = newVariable;
                }
                else if (!newVariable.Overridable)
                {
                    this.Messaging.Write(ErrorMessages.DuplicateLocalizationIdentifier(newVariable.SourceLineNumbers, newVariable.Id));
                }
            }

            foreach (var localizedControl in localization.LocalizedControls)
            {
                if (!controls.ContainsKey(localizedControl.Key))
                {
                    controls.Add(localizedControl.Key, localizedControl.Value);
                }
            }

            return new Localization(existingLocalization.Codepage ?? localization.Codepage, existingLocalization.SummaryInformationCodepage ?? localization.SummaryInformationCodepage, existingLocalization.Culture, variables, controls);
        }
    }
}
