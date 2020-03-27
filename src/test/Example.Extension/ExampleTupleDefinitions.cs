// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data;
    using WixToolset.Data.Burn;

    public static class ExampleTupleDefinitions
    {
        public const string ExampleName = "Example";

        public static readonly IntermediateTupleDefinition Example = new IntermediateTupleDefinition(
            ExampleName,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExampleTupleFields.Example), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExampleTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ExampleTuple));

        public static readonly IntermediateTupleDefinition ExampleSearch = new IntermediateTupleDefinition(
            nameof(ExampleSearch),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExampleTupleFields.Example), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExampleSearchTupleFields.SearchFor), IntermediateFieldType.String),
            },
            typeof(ExampleSearchTuple));

        static ExampleTupleDefinitions()
        {
            ExampleSearch.AddTag(BurnConstants.BundleExtensionSearchTupleDefinitionTag);
        }
    }
}
