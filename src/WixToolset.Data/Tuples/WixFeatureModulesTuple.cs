// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixFeatureModules = new IntermediateTupleDefinition(
            TupleDefinitionType.WixFeatureModules,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFeatureModulesTupleFields.Feature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFeatureModulesTupleFields.WixMerge_), IntermediateFieldType.String),
            },
            typeof(WixFeatureModulesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixFeatureModulesTupleFields
    {
        Feature_,
        WixMerge_,
    }

    public class WixFeatureModulesTuple : IntermediateTuple
    {
        public WixFeatureModulesTuple() : base(TupleDefinitions.WixFeatureModules, null, null)
        {
        }

        public WixFeatureModulesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixFeatureModules, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFeatureModulesTupleFields index] => this.Fields[(int)index];

        public string Feature_
        {
            get => (string)this.Fields[(int)WixFeatureModulesTupleFields.Feature_]?.Value;
            set => this.Set((int)WixFeatureModulesTupleFields.Feature_, value);
        }

        public string WixMerge_
        {
            get => (string)this.Fields[(int)WixFeatureModulesTupleFields.WixMerge_]?.Value;
            set => this.Set((int)WixFeatureModulesTupleFields.WixMerge_, value);
        }
    }
}