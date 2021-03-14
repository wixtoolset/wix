// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class SymbolDefinitionCreator : ISymbolDefinitionCreator
    {
        public SymbolDefinitionCreator(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        private IEnumerable<IExtensionData> ExtensionData { get; set; }

        private Dictionary<string, IntermediateSymbolDefinition> CustomDefinitionByName { get; } = new Dictionary<string, IntermediateSymbolDefinition>();

        public void AddCustomSymbolDefinition(IntermediateSymbolDefinition definition)
        {
            if (!this.CustomDefinitionByName.TryGetValue(definition.Name, out var existing) || definition.Revision > existing.Revision)
            {
                this.CustomDefinitionByName[definition.Name] = definition;
            }
        }

        public bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition)
        {
            // First, look in the built-ins.
            symbolDefinition = SymbolDefinitions.ByName(name);

            if (symbolDefinition == null)
            {
                if (this.ExtensionData == null)
                {
                    this.LoadExtensionData();
                }

                // Second, look in the extensions.
                foreach (var data in this.ExtensionData)
                {
                    if (data.TryGetSymbolDefinitionByName(name, out symbolDefinition))
                    {
                        break;
                    }
                }

                // Finally, look in the custom symbol definitions provided during an intermediate load.
                if (symbolDefinition == null)
                {
                    this.CustomDefinitionByName.TryGetValue(name, out symbolDefinition);
                }
            }

            return symbolDefinition != null;
        }

        private void LoadExtensionData()
        {
            var extensionManager = this.ServiceProvider.GetService<IExtensionManager>();

            this.ExtensionData = extensionManager.GetServices<IExtensionData>();
        }
    }
}
