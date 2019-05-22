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
                new IntermediateFieldDefinition(nameof(FeatureComponentsTupleFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureComponentsTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(FeatureComponentsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FeatureComponentsTupleFields
    {
        FeatureRef,
        ComponentRef,
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

        public string FeatureRef
        {
            get => (string)this.Fields[(int)FeatureComponentsTupleFields.FeatureRef];
            set => this.Set((int)FeatureComponentsTupleFields.FeatureRef, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)FeatureComponentsTupleFields.ComponentRef];
            set => this.Set((int)FeatureComponentsTupleFields.ComponentRef, value);
        }
    }
}