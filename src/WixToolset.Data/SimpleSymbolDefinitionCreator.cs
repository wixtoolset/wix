// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Collections.Generic;

    internal class SimpleSymbolDefinitionCreator : ISymbolDefinitionCreator
    {
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
            symbolDefinition = SymbolDefinitions.ByName(name);

            if (symbolDefinition == null)
            {
                symbolDefinition = this.CustomDefinitionByName.GetValueOrDefault(name);
            }

            return symbolDefinition != null;
        }
    }
}
