// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleVariable = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleVariable,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleVariableTupleFields.WixBundleVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleVariableTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleVariableTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleVariableTupleFields.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundleVariableTupleFields.Persisted), IntermediateFieldType.Bool),
            },
            typeof(WixBundleVariableTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleVariableTupleFields
    {
        WixBundleVariable,
        Value,
        Type,
        Hidden,
        Persisted,
    }

    public class WixBundleVariableTuple : IntermediateTuple
    {
        public WixBundleVariableTuple() : base(TupleDefinitions.WixBundleVariable, null, null)
        {
        }

        public WixBundleVariableTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleVariable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleVariableTupleFields index] => this.Fields[(int)index];

        public string WixBundleVariable
        {
            get => (string)this.Fields[(int)WixBundleVariableTupleFields.WixBundleVariable];
            set => this.Set((int)WixBundleVariableTupleFields.WixBundleVariable, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixBundleVariableTupleFields.Value];
            set => this.Set((int)WixBundleVariableTupleFields.Value, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)WixBundleVariableTupleFields.Type];
            set => this.Set((int)WixBundleVariableTupleFields.Type, value);
        }

        public bool Hidden
        {
            get => (bool)this.Fields[(int)WixBundleVariableTupleFields.Hidden];
            set => this.Set((int)WixBundleVariableTupleFields.Hidden, value);
        }

        public bool Persisted
        {
            get => (bool)this.Fields[(int)WixBundleVariableTupleFields.Persisted];
            set => this.Set((int)WixBundleVariableTupleFields.Persisted, value);
        }
    }
}