// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class TupleDefinitionCreator : ITupleDefinitionCreator
    {
        public TupleDefinitionCreator(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; }

        private IEnumerable<IExtensionData> ExtensionData { get; set; }

        private Dictionary<string, IntermediateTupleDefinition> CustomDefinitionByName { get; } = new Dictionary<string, IntermediateTupleDefinition>();

        public void AddCustomTupleDefinition(IntermediateTupleDefinition definition)
        {
            if (!this.CustomDefinitionByName.TryGetValue(definition.Name, out var existing) || definition.Revision > existing.Revision)
            {
                this.CustomDefinitionByName[definition.Name] = definition;
            }
        }

        public bool TryGetTupleDefinitionByName(string name, out IntermediateTupleDefinition tupleDefinition)
        {
            // First, look in the built-ins.
            tupleDefinition = TupleDefinitions.ByName(name);

            if (tupleDefinition == null)
            {
                if (this.ExtensionData == null)
                {
                    this.LoadExtensionData();
                }

                // Second, look in the extensions.
                foreach (var data in this.ExtensionData)
                {
                    if (data.TryGetTupleDefinitionByName(name, out tupleDefinition))
                    {
                        break;
                    }
                }

                // Finally, look in the custom tuple definitions provided during an intermediate load.
                if (tupleDefinition == null)
                {
                    this.CustomDefinitionByName.TryGetValue(name, out tupleDefinition);
                }
            }

            return tupleDefinition != null;
        }

        private void LoadExtensionData()
        {
            var extensionManager = (IExtensionManager)this.ServiceProvider.GetService(typeof(IExtensionManager));

            this.ExtensionData = extensionManager.GetServices<IExtensionData>();
        }
    }
}
