// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class ProcessPropertiesCommand
    {
        public ProcessPropertiesCommand(IntermediateSection section, WixPackageSymbol packageSymbol, int fallbackLcid, bool populateDelayedVariables, IBackendHelper backendHelper)
        {
            this.Section = section;
            this.PackageSymbol = packageSymbol;
            this.FallbackLcid = fallbackLcid;
            this.PopulateDelayedVariables = populateDelayedVariables;
            this.BackendHelper = backendHelper;
        }

        private IntermediateSection Section { get; }

        private WixPackageSymbol PackageSymbol { get; }

        private int FallbackLcid { get; }

        private bool PopulateDelayedVariables { get; }

        private IBackendHelper BackendHelper { get; }

        public Dictionary<string, string> DelayedVariablesCache { get; private set; }

        public string ProductLanguage { get; private set; }

        public void Execute()
        {
            PropertySymbol languageSymbol = null;
            var variableCache = this.PopulateDelayedVariables ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : null;

            if (SectionType.Package == this.Section.Type || variableCache != null)
            {
                foreach (var propertySymbol in this.Section.Symbols.OfType<PropertySymbol>())
                {
                    // Set the ProductCode if it is to be generated.
                    if ("ProductCode" == propertySymbol.Id.Id && "*".Equals(propertySymbol.Value, StringComparison.Ordinal))
                    {
                        propertySymbol.Value = this.BackendHelper.CreateGuid();
                    }
                    else if ("ProductLanguage" == propertySymbol.Id.Id)
                    {
                        languageSymbol = propertySymbol;
                    }

                    // Add the property name and value to the variableCache.
                    if (variableCache != null)
                    {
                        variableCache[$"property.{propertySymbol.Id.Id}"] = propertySymbol.Value;
                    }
                }

                if (this.Section.Type == SectionType.Package && String.IsNullOrEmpty(languageSymbol?.Value))
                {
                    if (languageSymbol == null)
                    {
                        languageSymbol = this.Section.AddSymbol(new PropertySymbol(this.PackageSymbol.SourceLineNumbers, new Identifier(AccessModifier.Section, "ProductLanguage")));
                    }

                    this.PackageSymbol.Language = this.FallbackLcid.ToString();
                    languageSymbol.Value = this.FallbackLcid.ToString();
                }
            }

            this.DelayedVariablesCache = variableCache;
            this.ProductLanguage = languageSymbol?.Value;
        }
    }
}
