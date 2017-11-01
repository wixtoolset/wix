// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    internal class SimpleTupleDefinitionCreator : ITupleDefinitionCreator
    {
        public bool TryGetTupleDefinitionByName(string name, out IntermediateTupleDefinition tupleDefinition)
        {
            tupleDefinition = TupleDefinitions.ByName(name);
            return tupleDefinition != null;
        }
    }
}