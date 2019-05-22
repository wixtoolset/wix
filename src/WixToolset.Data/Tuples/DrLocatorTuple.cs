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
                new IntermediateFieldDefinition(nameof(DrLocatorTupleFields.SignatureRef), IntermediateFieldType.String),
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
        SignatureRef,
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

        public string SignatureRef
        {
            get => (string)this.Fields[(int)DrLocatorTupleFields.SignatureRef];
            set => this.Set((int)DrLocatorTupleFields.SignatureRef, value);
        }

        public string Parent
        {
            get => (string)this.Fields[(int)DrLocatorTupleFields.Parent];
            set => this.Set((int)DrLocatorTupleFields.Parent, value);
        }

        public string Path
        {
            get => (string)this.Fields[(int)DrLocatorTupleFields.Path];
            set => this.Set((int)DrLocatorTupleFields.Path, value);
        }

        public int Depth
        {
            get => (int)this.Fields[(int)DrLocatorTupleFields.Depth];
            set => this.Set((int)DrLocatorTupleFields.Depth, value);
        }
    }
}