// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Burn;

    public enum ExampleSymbolDefinitionType
    {
        Example,
        ExampleSearch,
    }

    public static class ExampleSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Example = new IntermediateSymbolDefinition(
            ExampleSymbolDefinitionType.Example.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExampleSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ExampleSymbol));

        public static readonly IntermediateSymbolDefinition ExampleSearch = new IntermediateSymbolDefinition(
            ExampleSymbolDefinitionType.ExampleSearch.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExampleSearchSymbolFields.SearchFor), IntermediateFieldType.String),
            },
            typeof(ExampleSearchSymbol));

        static ExampleSymbolDefinitions()
        {
            ExampleSearch.AddTag(BurnConstants.BundleExtensionSearchSymbolDefinitionTag);
        }

        public static bool TryGetSymbolType(string name, out ExampleSymbolDefinitionType type)
        {
            return Enum.TryParse(name, out type);
        }

        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!TryGetSymbolType(name, out var type))
            {
                return null;
            }
            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(ExampleSymbolDefinitionType type)
        {
            switch (type)
            {
                case ExampleSymbolDefinitionType.Example:
                    return ExampleSymbolDefinitions.Example;

                case ExampleSymbolDefinitionType.ExampleSearch:
                    return ExampleSymbolDefinitions.ExampleSearch;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
