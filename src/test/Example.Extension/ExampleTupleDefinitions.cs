// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Burn;

    public enum ExampleTupleDefinitionType
    {
        Example,
        ExampleSearch,
    }

    public static class ExampleTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Example = new IntermediateTupleDefinition(
            ExampleTupleDefinitionType.Example.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExampleTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ExampleTuple));

        public static readonly IntermediateTupleDefinition ExampleSearch = new IntermediateTupleDefinition(
            ExampleTupleDefinitionType.ExampleSearch.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExampleSearchTupleFields.SearchFor), IntermediateFieldType.String),
            },
            typeof(ExampleSearchTuple));

        static ExampleTupleDefinitions()
        {
            ExampleSearch.AddTag(BurnConstants.BundleExtensionSearchTupleDefinitionTag);
        }

        public static bool TryGetTupleType(string name, out ExampleTupleDefinitionType type)
        {
            return Enum.TryParse(name, out type);
        }

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!TryGetTupleType(name, out var type))
            {
                return null;
            }
            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(ExampleTupleDefinitionType type)
        {
            switch (type)
            {
                case ExampleTupleDefinitionType.Example:
                    return ExampleTupleDefinitions.Example;

                case ExampleTupleDefinitionType.ExampleSearch:
                    return ExampleTupleDefinitions.ExampleSearch;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
