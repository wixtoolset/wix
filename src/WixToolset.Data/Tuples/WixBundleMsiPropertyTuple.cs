// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleMsiProperty = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleMsiProperty,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMsiPropertyTupleFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPropertyTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPropertyTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPropertyTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(WixBundleMsiPropertyTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleMsiPropertyTupleFields
    {
        PackageRef,
        Name,
        Value,
        Condition,
    }

    public class WixBundleMsiPropertyTuple : IntermediateTuple
    {
        public WixBundleMsiPropertyTuple() : base(TupleDefinitions.WixBundleMsiProperty, null, null)
        {
        }

        public WixBundleMsiPropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleMsiProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsiPropertyTupleFields index] => this.Fields[(int)index];

        public string PackageRef
        {
            get => (string)this.Fields[(int)WixBundleMsiPropertyTupleFields.PackageRef];
            set => this.Set((int)WixBundleMsiPropertyTupleFields.PackageRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleMsiPropertyTupleFields.Name];
            set => this.Set((int)WixBundleMsiPropertyTupleFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixBundleMsiPropertyTupleFields.Value];
            set => this.Set((int)WixBundleMsiPropertyTupleFields.Value, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixBundleMsiPropertyTupleFields.Condition];
            set => this.Set((int)WixBundleMsiPropertyTupleFields.Condition, value);
        }
    }
}