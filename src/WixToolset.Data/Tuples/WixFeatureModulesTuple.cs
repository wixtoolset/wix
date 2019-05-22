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
                new IntermediateFieldDefinition(nameof(WixFeatureModulesTupleFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFeatureModulesTupleFields.WixMergeRef), IntermediateFieldType.String),
            },
            typeof(WixFeatureModulesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixFeatureModulesTupleFields
    {
        FeatureRef,
        WixMergeRef,
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

        public string FeatureRef
        {
            get => (string)this.Fields[(int)WixFeatureModulesTupleFields.FeatureRef];
            set => this.Set((int)WixFeatureModulesTupleFields.FeatureRef, value);
        }

        public string WixMergeRef
        {
            get => (string)this.Fields[(int)WixFeatureModulesTupleFields.WixMergeRef];
            set => this.Set((int)WixFeatureModulesTupleFields.WixMergeRef, value);
        }
    }
}