// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    internal class ExampleExtensionData : IExtensionData
    {
        public string DefaultCulture => null;

        public Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return Intermediate.Load(typeof(ExampleExtensionData).Assembly, "Example.Extension.Example.wixlib", symbolDefinitions);
        }

        public bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition)
        {
            symbolDefinition = ExampleSymbolDefinitions.ByName(name);
            return symbolDefinition != null;
        }
    }
}