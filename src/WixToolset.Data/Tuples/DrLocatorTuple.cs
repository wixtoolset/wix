// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition DrLocator = new IntermediateTupleDefinition(
            TupleDefinitionType.DrLocator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(DrLocatorTupleFields.Signature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DrLocatorTupleFields.Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DrLocatorTupleFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DrLocatorTupleFields.Depth), IntermediateFieldType.Number),
            },
            typeof(DrLocatorTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum DrLocatorTupleFields
    {
        Signature_,
        Parent,
        Path,
        Depth,
    }

    public class DrLocatorTuple : IntermediateTuple
    {
        public DrLocatorTuple() : base(TupleDefinitions.DrLocator, null, null)
        {
        }

        public DrLocatorTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.DrLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[DrLocatorTupleFields index] => this.Fields[(int)index];

        public string Signature_
        {
            get => (string)this.Fields[(int)DrLocatorTupleFields.Signature_]?.Value;
            set => this.Set((int)DrLocatorTupleFields.Signature_, value);
        }

        public string Parent
        {
            get => (string)this.Fields[(int)DrLocatorTupleFields.Parent]?.Value;
            set => this.Set((int)DrLocatorTupleFields.Parent, value);
        }

        public string Path
        {
            get => (string)this.Fields[(int)DrLocatorTupleFields.Path]?.Value;
            set => this.Set((int)DrLocatorTupleFields.Path, value);
        }

        public int Depth
        {
            get => (int)this.Fields[(int)DrLocatorTupleFields.Depth]?.Value;
            set => this.Set((int)DrLocatorTupleFields.Depth, value);
        }
    }
}