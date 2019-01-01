// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Collections.Generic;

    internal class SimpleTupleDefinitionCreator : ITupleDefinitionCreator
    {
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
            tupleDefinition = TupleDefinitions.ByName(name);

            if (tupleDefinition == null)
            {
                tupleDefinition = this.CustomDefinitionByName.GetValueOrDefault(name);
            }

            return tupleDefinition != null;
        }
    }
}
