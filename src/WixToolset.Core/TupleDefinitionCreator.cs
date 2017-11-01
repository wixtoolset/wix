// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
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

        public bool TryGetTupleDefinitionByName(string name, out IntermediateTupleDefinition tupleDefinition)
        {
            tupleDefinition = TupleDefinitions.ByName(name);

            if (tupleDefinition == null)
            {
                if (this.ExtensionData == null)
                {
                    this.LoadExtensionData();
                }

                foreach (var data in this.ExtensionData)
                {
                    if (data.TryGetTupleDefinitionByName(name, out tupleDefinition))
                    {
                        break;
                    }
                }
            }

            return tupleDefinition != null;
        }

        private void LoadExtensionData()
        {
            var extensionManager = (IExtensionManager)this.ServiceProvider.GetService(typeof(IExtensionManager));

            this.ExtensionData = extensionManager.Create<IExtensionData>();
        }
    }
}
