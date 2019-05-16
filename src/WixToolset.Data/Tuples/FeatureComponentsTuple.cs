// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition FeatureComponents = new IntermediateTupleDefinition(
            TupleDefinitionType.FeatureComponents,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FeatureComponentsTupleFields.Feature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureComponentsTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(FeatureComponentsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FeatureComponentsTupleFields
    {
        Feature_,
        Component_,
    }

    public class FeatureComponentsTuple : IntermediateTuple
    {
        public FeatureComponentsTuple() : base(TupleDefinitions.FeatureComponents, null, null)
        {
        }

        public FeatureComponentsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.FeatureComponents, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FeatureComponentsTupleFields index] => this.Fields[(int)index];

        public string Feature_
        {
            get => (string)this.Fields[(int)FeatureComponentsTupleFields.Feature_];
            set => this.Set((int)FeatureComponentsTupleFields.Feature_, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)FeatureComponentsTupleFields.Component_];
            set => this.Set((int)FeatureComponentsTupleFields.Component_, value);
        }
    }
}