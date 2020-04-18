// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    internal class ExampleExtensionData : IExtensionData
    {
        public string DefaultCulture => null;

        public Intermediate GetLibrary(ITupleDefinitionCreator tupleDefinitions)
        {
            return Intermediate.Load(typeof(ExampleExtensionData).Assembly, "Example.Extension.Example.wixlib", tupleDefinitions);
        }

        public bool TryGetTupleDefinitionByName(string name, out IntermediateTupleDefinition tupleDefinition)
        {
            tupleDefinition = ExampleTupleDefinitions.ByName(name);
            return tupleDefinition != null;
        }
    }
}