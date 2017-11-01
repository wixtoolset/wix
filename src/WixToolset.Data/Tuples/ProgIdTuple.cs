// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ProgId = new IntermediateTupleDefinition(
            TupleDefinitionType.ProgId,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.ProgId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.ProgId_Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.Class_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.Icon_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdTupleFields.IconIndex), IntermediateFieldType.Number),
            },
            typeof(ProgIdTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ProgIdTupleFields
    {
        ProgId,
        ProgId_Parent,
        Class_,
        Description,
        Icon_,
        IconIndex,
    }

    public class ProgIdTuple : IntermediateTuple
    {
        public ProgIdTuple() : base(TupleDefinitions.ProgId, null, null)
        {
        }

        public ProgIdTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ProgId, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ProgIdTupleFields index] => this.Fields[(int)index];

        public string ProgId
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.ProgId]?.Value;
            set => this.Set((int)ProgIdTupleFields.ProgId, value);
        }

        public string ProgId_Parent
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.ProgId_Parent]?.Value;
            set => this.Set((int)ProgIdTupleFields.ProgId_Parent, value);
        }

        public string Class_
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.Class_]?.Value;
            set => this.Set((int)ProgIdTupleFields.Class_, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.Description]?.Value;
            set => this.Set((int)ProgIdTupleFields.Description, value);
        }

        public string Icon_
        {
            get => (string)this.Fields[(int)ProgIdTupleFields.Icon_]?.Value;
            set => this.Set((int)ProgIdTupleFields.Icon_, value);
        }

        public int IconIndex
        {
            get => (int)this.Fields[(int)ProgIdTupleFields.IconIndex]?.Value;
            set => this.Set((int)ProgIdTupleFields.IconIndex, value);
        }
    }
}